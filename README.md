# ModNationServer
Open source C# server for modnation racers
NOTE: This version will be deprecated soon, the server is being rewritten and development is continuing elsewhere

Currently handles regular http and SSL traffic (You will need to supply your own PKCS#12 certificate (can be self signed) and put it with the server executable.
IMPORTANT: You will also need to enable RC4 cipher suite support on your server for the game to properly authenticate with the SSL server, run "Enable-TlsCipherSuite TLS_RSA_WITH_RC4_128_MD5" under an administrator PowerShell prompt.

Currently implemented:
- All sharestation features apart from search (but its very messy and needs a lot of clean up)
- SSL server can send list of services to game and game attempts connection, need to emulate the services.
- Messaging system (by working I mean it dumps the message into the database and thats about it)

To implement:
- ~~Fix stupid bug with sharestation where it complains about "playerconnect disconnect" when selecting download even though it doesnt request anything?~~
- ~~Directory server responses~~
- ~~Figure out what the Bombd server does (not quite up to the stage where it requests for it yet)~~
- Properly implement messaging, profiles, friend lists, achievements (Partially done)
- Link stuff to SQL database properly
- Clean up messy code (theres a lot)
- Also possibly switch to something like mySQL to improve database performance
