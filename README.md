# uptobox-dl

## Overview

An Uptobox batch downloader. Uptobox allows, for free members, 30 min to wait before each download.

With `uptobox-dl`, you're able to download multiple uptobox/uptostream links without any other action other than grabbing the links and running `uptobox-dl`.

*Note: Uptobox is a file hosting provider*.

## Example

```
$ ./uptobox-dl -t <my_user_token> https://uptobox.com/<filecode_1> https://uptostream.com/iframe/<filecode_2>
Start processing https://uptobox.com/<filecode_1>
Got waiting token, awaiting for 00:08:46 - until 3:17:23 PM
Got waiting token, awaiting for 00:00:31 - until 3:17:54 PM
396752005B/396752005B: 100%
Downloaded <my_file_1.ext>

Start processing https://uptostream.com/iframe/<filecode_2>
Got waiting token, awaiting for 00:25:42 - until 3:47:55 PM
```

## Usage
`./uptobox-dl -t <my_user_token> [my_links...]`
```
./uptobox-dl --help
uptobox-dl 1.0.0

  -v, --verbose    Set output to verbose messages.

  -d, --debug      Print debug data.

  -t, --token      Required. Uptobox user token. See https://docs.uptobox.com/?javascript#how-to-find-my-api-token

  --help           Display this help screen.

  --version        Display version information.

  value pos. 0     Uptobox links to download
```

**Why do I need a user token?**

It allows you to speed up the time waiting for downloads (30min between each download instead of >1h). It's free, you just need to create an Uptobox account.