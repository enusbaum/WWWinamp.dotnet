# WWWinamp 4.1 Build 2787

This is the last version of the WWWinamp 4 source code I could find.

I wrote this version of WWWinamp to replace WWWinamp 3.x which was written in C and based on the original source code by Nullsoft.

The decision to rewrite WWWinamp was really twofold. First, I was a C# developer by trade so it made the most sense to me. Second, I was never comfortable running a custom Web Server written in C. That just felt like I was asking for trouble.

This version was originally written on .Net 2.0 but later enhanced to 3.0 to add a WCF endpoint for API access (long before REST and JSON were a popular thing). Additionally, this was written before http.sys so the Web Server itself is custom written.

I'm unsure if this still works on the latest versions of Windows/WinAMP.

Cheers!
