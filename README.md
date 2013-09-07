Carpool
=======
Carpool implements some algorithms for running a fair carpool. 
The basis for the default algorithm is the 1983 paper 
[A Fair Carpool Scheduling Algorithm](http://researcher.watson.ibm.com/researcher/files/us-fagin/ibmj83.pdf)
by Ronald Fagin and John H Williams.

Building
--------
In your working copy run: ``xbuild /property:Configuration=Release``. The resulting binary works on all platforms with Mono or .NET 4.

Running
-------
Run without arguments for usage details ``mono Carpool.exe``. 

Create an input file ``pool1.txt``. The first line represents the members of your carpool. The remaining lines represent drives.
The first character of each drive designates the driver and the rest of the characters are for the riders. Anything after a whitespace character on each drive line is a comment.
```
S Stan K Kenny Y Kyle E Eric L Leopold

SKYL  2013-05-04
ESKY  this is just a comment
KE    these don't do anything
YKSE
LEY
SEKL
KELSY
YS
ESK
```

Run ``mono Carpool.exe pool1.txt`` to use the default suggestion method:
```
Using method: Units

  PERSON->      Stan     Kenny      Kyle      Eric   Leopold
                  -2        -2        13        -7        -2

Given Stan, Kenny, Kyle, Eric, Leopold show up...
	First Eric
	Then Stan or Kenny or Leopold
	Then Kyle
```

Run with the characters of the participants to limit suggestions ``mono Carpool.exe pool1.txt KEY``:
```
Given Kenny, Eric, Kyle show up...
	First Eric
	Then Kenny
	Then Kyle
```
To see the how "fair" you current situation is choose the subsets method and enable verbose output ``mono Carpool.exe -m subsets -v pool1.txt``. 
The suggestions will be based on the subsets method. If you're not using subsets normally, you can ignore the suggestion. 
The fairness calculation applies regardless of the method you use to manage your carpool.
```
Using method: Subsets

  DRIVER->      Stan     Kenny      Kyle      Eric   Leopold
     SKYEL         0         1         0         0         0
      SKYL         1         0         0                   0
      SKYE         0         0         1         1          
      SKEL         1         0                   0         0
       YEL                             0         0         1
       SKE         0         0                   1          
        KE                   1                   0          
        SY         0                   1                    

Participations in pool size:
        b5         1         1         1         1         1
        b4         4         4         3         3         2
        b3         1         1         1         2         1
        b2         1         1         1         1         0

Drives in pool size:
        d5         0         1         0         0         0
        d4         2         0         1         1         0
        d3         0         0         0         1         1
        d2         0         1         1         0         0

Current Fairness:
    fair d         2         2       1.8       2.1         1
  actual d         2         2         2         2         1

Given Stan, Kenny, Kyle, Eric, Leopold show up...
	First Eric or Leopold or Stan or Kyle
	Then Kenny
```
This shows Kyle has driven (actual d) slightly more than his fair share (fair d). 
Eric is slighty below his fair share. The agrees with the suggestions from the units method that Eric needs to drive next the most and Kyle the least.
