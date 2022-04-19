# EpgMgr
XmlTV Generator framework with plugin support

This project came about because I became somewhat annoyed when a well known xmltv tool turned to requiring donations to actually do anything useful. Requiring donations even from people that had created configurations for their tool previously. It was also quite slow anyway and I figured it was time to make something open source that could do the same, but faster.

I was also spurned on further when I found that nameless program captures quite some data about your machine/user details and uses it as a "Hardware ID". Except, they don't hash it, they just encrpyt it and send all your info to their servers. Without a doubt, there needed to be a replacement. I hope one day this can be it.

This is the initial result of that. Ultimately the plan is that it will allow the creation of fairly simple C# plugins that can be enabled and configured to create a final XMLTV file containing channels and programs from all the enabled plugins.

# Use
Currently there's only a Demo Plugin and Sky UK. It's entirely console driven right now. I've tried to make it so that it won't be a huge job to make a UI for it later. But, since most of the use of a tool like this is to be run on a scheduler, I figured a console/command line made the most sense.

When the program first starts it will have a default configuration and no plugins enabled. The first step is to enable a plugin. You can see all commands available by typing "help" and can get usage info with "help <command>". For plugins you will want to use "plugin list all" this will show all plugins that are detected in the plugin folder. With that info you can use "plugin enable <plugin name/guid>"

Configuration can be saved with "save". You should save after enabling a plugin, so that the enabled state is saved. Plugins can have their own commands, and those will usually be in the plugin folder. You change folder and list values just like a shell. "cd /config/plugins/demo" will take you to the demo plugin configuration for example. "ls" will list values that can be set with the "set" command. Otherwise typing "help" in a plugin folder will show local commands which may aid with configuration (such as finding channels, enabling channels and setting aliases).

Finally "run" will run the process for all plugins with the current configuration and produce an xmltv file.

Very shortly (hopefully before anyone reads this) there will be a -run command line option to bypass the console and just run the xmltv create process.
