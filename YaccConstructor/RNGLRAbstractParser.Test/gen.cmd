..\YaccConstructor\bin\Release\YaccConstructor.exe -i SimpleCalc.yrd -c ExpandEbnf -c ExpandMeta ^
        -g "RNGLRGenerator -pos int -token int -module RNGLR.ParseSimpleCalc -translate true -table LR -o SimpleCalc.yrd.fs" >> log.txt

..\YaccConstructor\bin\Release\YaccConstructor.exe -i Calc.yrd -c ExpandEbnf -c ExpandMeta ^
        -g "RNGLRGenerator -pos int -token int -module RNGLR.ParseCalc -translate true -table LR -o Calc.yrd.fs" >> log.txt

..\YaccConstructor\bin\Release\YaccConstructor.exe -i simpleCalc_with_Nterms.yrd -c ExpandEbnf -c ExpandMeta ^
        -g "RNGLRGenerator -pos int -token int -module RNGLR.SimpleCalcWithNTerm -translate true -table LR -o simpleCalc_with_Nterms.yrd.fs" >> log.txt

