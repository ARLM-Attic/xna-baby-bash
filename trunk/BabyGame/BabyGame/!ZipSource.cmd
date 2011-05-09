cd ..
cd ..

REM TODO: add exclusions
BabyGame\BabyGameContent\zip.exe -ur9 BabyGame\BabyGame\BabyBashXNA_Source.zip *.*

cd BabyGame
cd BabyGameContent
zip.exe -u9 ..\BabyGame\BabyBashXNA.zip LICENSE.txt NOTICE.txt