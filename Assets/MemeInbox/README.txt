Drop new meme pictures into this folder (png / jpg).

Name every file one of these ways:
  001_Drake_Hotline_Bling.jpg     the number picks the rarity (see the lists in Assets/Editor/MemeImporter.cs)
  rare_Distracted Boyfriend.png   or write the rarity yourself: common, rare, epic or legendary

Then in Unity use the menu:  Meme Hunter > Import memes from MemeInbox
It moves the pictures to Assets/mst_files/Pictures/Memes, sets them up as sprites and creates the meme
data in Assets/Resources/Memes (the game picks them up automatically). Files with a wrong name are skipped
and listed in the Console.
