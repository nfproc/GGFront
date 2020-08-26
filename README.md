GGFront: A GHDL/GTKWave GUI Frontend
====================================

注意 (Notice)
-------------

GGFront は基本的に<a href="https://aitech.ac.jp/~dslab/nf/ggfront.html">作者のサイト</a>で公開している配布パッケージの形で利用されることを想定しています．開発者の便宜のために，ソースコードをここでも公開しています．なお，現在 GGFront は日本語にのみ対応しています．

GGFront is basically supposed to be used in a form of the distribution package that can be downloaded from <a href="https://aitech.ac.jp/~dslab/nf/ggfront.html">the author's website</a>. The author makes the source code available also here for the sake of convenience of developers. Currently, GGFront only supports Japanese (ja).

概要
----

ハードウェア記述言語（HDL）の習得は，ディジタル回路の本格的な設計を学ぶために重要ですが，HDL の演習を手軽に行える環境を整備することは容易ではありませんでした．よく用いられる HDL の1つである VHDL のシミュレーションには，フリーソフトウェアのシミュレータ GHDL を波形ビューア GTKWave と組み合わせる方法があります．しかしこの方法は，適切なオプションをつけて GHDL を複数回実行する必要があること，またそもそも GHDL が CUI のツールであることから，主に Windows を利用する学生にとっては扱いづらいものでした．

GGFrontは，上記の問題を解決し，VHDLを用いたディジタル回路設計演習を手軽かつポータブルに実施できる環境を提供するためのフロントエンドツールです．

配布パッケージのダウンロード，使用方法などは<a href="https://aitech.ac.jp/~dslab/nf/ggfront.html">作者のサイト</a>を参照してください．