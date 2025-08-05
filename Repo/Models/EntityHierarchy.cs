// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System.Collections.Generic;
using System.IO;
using GGFront.ViewModels;

namespace GGFront.Models
{
    public class EntityHierarchy
    {
        public List<EntityHierarchyItem> Items;
        private GGFrontProject Project;
        public bool IsVHDL2008;

        public EntityHierarchy(GGFrontProject parentProject)
        {
            Items = new List<EntityHierarchyItem>();
            Project = parentProject;
            IsVHDL2008 = false;
        }

        // ソースを解析して Entity の階層関係を作成
        public List<EntityHierarchyItem> Update()
        {
            Dictionary<string, string> inFile = new Dictionary<string, string>();
            Dictionary<string, List<string>> duplicatedEntities = new Dictionary<string, List<string>>();
            List<string> entities = new List<string>();
            Items = new List<EntityHierarchyItem>();
            List<VHDLSource.ComponentDecl> components = new List<VHDLSource.ComponentDecl>();
            IsVHDL2008 = false;

            // Entity, Component 宣言を数え上げる
            foreach (string FileName in Project.SourceFiles)
            {
                VHDLSource src = new VHDLSource(FileName);
                if (! src.IsValid)
                    continue;
                IsVHDL2008 = IsVHDL2008 || src.IsVHDL2008;
                foreach (string entity in src.Entities)
                {
                    if (entities.Contains(entity))
                    {
                        if (! duplicatedEntities.ContainsKey(entity))
                        {
                            duplicatedEntities[entity] = new List<string>();
                            duplicatedEntities[entity].Add(inFile[entity]);
                        }
                        duplicatedEntities[entity].Add(FileName);
                    }
                    else
                    {
                        entities.Add(entity);
                        inFile[entity] = FileName;
                    }
                }
                foreach (VHDLSource.ComponentDecl component in src.Components)
                    components.Add(component);
            }
            if (entities.Count == 0)
                return InvalidHierarchy("<!> Entity が見つかりません．");
            if (duplicatedEntities.Count != 0)
                return ReportDuplicatedEntities(duplicatedEntities);

            // 他から参照されていない Entity を列挙
            List<string> roots = new List<string>(entities);
            foreach (VHDLSource.ComponentDecl component in components)
                if (component.Name != null && roots.Contains(component.Name))
                    roots.Remove(component.Name);
            if (roots.Count == 0)
                return InvalidHierarchy("<!> Entity の循環参照を検出しました．");

            // 参照をたどり，木を生成
            List<List<EntityHierarchyItem>> trees = new List<List<EntityHierarchyItem>>();
            foreach (string root in roots)
            {
                List<EntityHierarchyItem>? tree = SearchEntityTree(root, new List<string>(), entities, components);
                if (tree == null)
                    return InvalidHierarchy("<!> Entity の循環参照を検出しました．");
                trees.Add(tree);
            }
            trees.Sort((a, b) => b.Count - a.Count);

            // トップモジュール・波形ファイルの設定
            if (! entities.Contains(Project.TopModule) || Project.GuessTopModule)
            {
                Project.TopModule = trees[0][0].Name ?? "";
                Project.GuessTopModule = true;
            }
            if (entities.Contains(Project.TopModule))
            {
                string file = inFile[Project.TopModule];
                int pos = file.LastIndexOf(".");
                pos = (pos == -1) ? file.Length : pos;
                Project.WavePath = Path.GetDirectoryName(file) + "/" + Path.GetFileNameWithoutExtension(file) + ".vcd";
            }

            // 各 Entity に対応するソースのパス名を設定
            foreach (List<EntityHierarchyItem> tree in trees)
            {
                foreach (EntityHierarchyItem item in tree)
                {
                    if (item.Name != null && inFile.ContainsKey(item.Name))
                    {
                        item.LongPath = inFile[item.Name];
                        item.ShortPath = Path.GetFileName(item.LongPath);
                    }
                    item.IsTop = item.Name != null && item.Name.Equals(Project.TopModule);
                }
                Items.AddRange(tree);
            }
            return Items;
        }

        // 指定された entity がトップモジュールから参照されているかを返す
        public bool Referenced(string entityName)
        {
            int TopLevel = -1;
            foreach (EntityHierarchyItem item in Items)
            {
                if (item.IsTop) // トップモジュールを見つけたら，そのレベルを記憶
                    TopLevel = item.Level;
                else if (item.Level <= TopLevel) // 同レベルか上位の entity に達したら記憶終了
                    TopLevel = -1;
                if (item.Name == entityName && TopLevel != -1)
                    return true;
            }
            return false;
        }

        // target からの参照関係を出力
        private List<EntityHierarchyItem>? SearchEntityTree(string target, List<string> parents, List<string> entities,
                                                            List<VHDLSource.ComponentDecl> components)
        {
            List<EntityHierarchyItem> result = new List<EntityHierarchyItem>();
            if (parents.Contains(target))  // 循環参照の場合エラー
                return null;

            EntityHierarchyItem targetItem = new EntityHierarchyItem
            {
                IsValid = entities.Contains(target), // Entity 宣言がない場合無効
                Level = 0,
                Name = target
            };
            result.Add(targetItem);
            if (! entities.Contains(target)) // Entity 宣言がない場合はそれ以上掘らない
            {
                targetItem.ShortPath = "???";
                return result;
            }

            List<string> newParents = new List<string>(parents);
            newParents.Add(target);
            foreach (VHDLSource.ComponentDecl component in components)
                if (component.From != null && component.Name != null && component.From.Equals(target))
                {
                    List<EntityHierarchyItem>? children = SearchEntityTree(
                        component.Name, newParents, entities, components);
                    if (children == null)
                        return null;
                    foreach (EntityHierarchyItem child in children)
                        child.Level += 1;
                    result.AddRange(children);
                }
            return result;
        }

        private List<EntityHierarchyItem> InvalidHierarchy(string message)
        {
            Items = new List<EntityHierarchyItem>();
            EntityHierarchyItem item = new EntityHierarchyItem
            {
                IsValid = false,
                Level = 0,
                Name = message
            };
            Items.Add(item);
            Project.TopModule = "";
            return Items;
        }

        private List<EntityHierarchyItem> ReportDuplicatedEntities(Dictionary<string, List<string>> dup)
        {
            Items = new List<EntityHierarchyItem>();
            EntityHierarchyItem top = new EntityHierarchyItem
            {
                IsValid = false,
                Level = 0,
                Name = "<!> Entity が重複して定義されています．"
            };
            Items.Add(top);
            foreach (KeyValuePair<string, List<string>> kvp in dup)
                foreach (string source in kvp.Value)
                {
                    EntityHierarchyItem sourceItem = new EntityHierarchyItem
                    {
                        IsValid = false,
                        Level = 1,
                        Name = kvp.Key,
                        LongPath = source,
                        ShortPath = Path.GetFileName(source)
                    };
                    Items.Add(sourceItem);
                }
            Project.TopModule = "";
            return Items;
        }
    }
}

