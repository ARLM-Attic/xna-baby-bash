Welcome to the innards of the default BabyPackage!

If you're here you've probably already guessed that BabyPackages are no more than renamed ZIP files.

The rules with BabyPackages:
1. They are ZIP files with either .zip or .babypackage extension.
2. They must contain a file called 'BabyPackage.xml' in the root of the ZIP archive.
3. BabyPackage.xml must conform to the BabyPackage schema (sorry, no DTD, the schema is by convention only).
4. The archive should contain various art and sound assets (they can be stored in folders if you want).

Here are some hints to help you create your own BabyPackages.
1. Check out the BabyPackage.xml in this BabyPackage. It documents most of the schema.
2. The 'CreateBabyPackage.cmd' file is the script used to create this BabyPackage (it assumes the infozip zip.exe file is in the current folder).
3. It's recommended to use an uncompressed ZIP file for faster loading (~20% faster vs ~20 smaller).
4. MP3s are decoded using a rather old, and not brilliantly optimised, entirely managed library. Depending on the number of CPU cores available, and if you're running on battery or not, loaded can take any where from 20% to 400% longer than with WAV files.
5. So you have a trade-off between speed and size; MP3s are slowest but smallest, zip compression of WAV is ~20% but slows loading a bit, stored WAV files is fastest but largest.


Enjoy creating your own BabyPackages!
Murray Grant
