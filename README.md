# Optimizely-Fix-Broken-Names
C# program that utilizes Optimizely's configured commerce v1 api to get product name's from a company's website using an administrator login and check if the products have symbols in the name that could limit customer searches

Changes product titles to replace UTF8 characters that users don't have on their keyboard (ie. ” is replaced with " or ¾ is replaced with 3/4) this makes searching much more efficient.  This program also takes the product title and replaces those symbols with keyboard equivalents, as well as removing symbols that most users would not search using (commas, dashes, ™, ®, etc.) and adds this new string as a search keyword to the product so those symbols are not removed from the original title.

As of 6/24/2024 the program has the ability to make those changes in the optimizely databse but the code is simply commented out and is instead just outputted to a tab delimited text file so the potential changes can be viewed first before making the changes.
