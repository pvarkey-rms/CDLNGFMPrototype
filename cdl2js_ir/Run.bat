@echo off

taskkill /f /im "node.exe"

IF "%1"=="" (
     SET port=3003
) ELSE (
     SET port=%1
)

start /B cake buildall

PING 127.0.0.1 -n 3 >nul

mv src/grammar/grammar-ast.js public/scripts/grammar-ast.js
cp public/lib/product.js public/scripts/product.js
cp public/lib/controller.js public/scripts/controller.js

PING 127.0.0.1 -n 1 >nul

start /B coffee app.coffee %port%




