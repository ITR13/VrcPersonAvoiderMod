# Vrc Person Avoider Mod
Ever accidentally joined a world containing somebody you're trying to avoid? Now you can get notified immediately when you join! Also notifies if they join a world you're in, and works no matter if you're blocked or they change name! 
NB: This mod uses the source and assets of [Knah's JoinNotifier](https://github.com/knah/VRCMods/tree/master/JoinNotifier)!

### How it works
First time you launch the game "AlertList.txt" will be added to your UserData folder, which you can modify while the game is open. In it you can put the usernames and userids of the people you want to be alerted of, one on each line.  
To get somebody's userid or username first go to the website and find the profile of the person you want to be alerted about. Next copy either their user id or username. You *can* use their display name too, but because this can change it's better to use one of the two other.  
![Screenshot showing what the user id, username, and display name of a user is](https://raw.githubusercontent.com/ITR13/VrcPersonAvoiderMod/master/website.png)  
Afterwards paste this into AlertList.txt, with at most one per line:
![Screenshot of AlertList.txt, showing the user id, username, and display name on separate lines](https://raw.githubusercontent.com/ITR13/VrcPersonAvoiderMod/master/txt.png)  
People will only create one notification no matter how many times they're found in the list. The list is not case-sensitive, and ignores whitespace in the beginning and end of the line. It might be useful to paste the username above the username so you can check who the person was if they ever change name, if combined with LogToConsole. NB: If somebody merges a steam account with a vrc account the username and user id might change!  

##### Other settings
- If you want to change the audio that's played when somebody joins you can add an audio file named PA-Join.ogg in the userdata folder.  
- In the melonpreferences.cgf file you can change the following settings under \[PersonAvoider\]
 - BlinkIcon: Show and blink the hud icon whenever you are notified
 - PlaySound: Play the chime sound whenever you are notified
 - ShowJoinedName: Show the name of the joined person
 - SoundVolume: The volume of the chime
 - UseUiMixer: Multiplies SoundVolume with the vrchat Ui volume slider
 - TextSize: Font size for the shown names
 - JoinColor: Color of the hud icon
 - LogToConsole: Log notifications in the console with their matched id.


### Misc
Want to contribute?
Feel free to create a different JoinIcon or sound to use in the project. PRs for layout changes, or adding a "alert on join" button on the in-game profile view would be nice.  

Any questions? Contact ITR#2941 on Discord