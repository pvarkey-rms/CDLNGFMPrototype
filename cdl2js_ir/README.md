cdl2js
======

<pre>
    _____________________
   /|                    |       JISON  |\              JSON OUTPUT
  /_|   __    __         |       PARSER |\\
  |    /  \  |  \  |     |       _______| \\            Declarations
  |   |      |   | |     |      |          \\           Covers
  |   |      |   | |     |      |_______   //              C1
  |    \__/  |__/  |____ |              | //               C2
  |                      |              |//             Sections
  |______________________|              |/               ........
</pre>


A web-based RMS(one) CDL parser that contains:

* A node.js module that serves as a proof-of-concept prototype.
* A Javascript parser generated using JISON.

INSTALL
-------
1. create 'scripts' directory inside 'public'
2. npm install which
3. npm install express
4. npm install jasmine-node -g

DEPLOY
------
For Windows: 

        Run.bat 3000


TEST
----

		jasmine-node --captureExceptions --verbose --coffee spec/


