@REM  Name: 递归删除指定的目录，请把此文件放在你希望执行的那个目录   
@echo off  
setlocal enabledelayedexpansion  
  
@REM 设置你想删除的目录  
set WHAT_SHOULD_BE_DELETED=bin 
set IN_LOOP=no 

:del
for /r %~dp0 %%a in (!WHAT_SHOULD_BE_DELETED!) do (  
  if exist %%a (  
  echo "删除"%%a   
  rd /s /q "%%a"  
 )  
)
if %IN_LOOP%  == yes goto exit

set WHAT_SHOULD_BE_DELETED=obj 
set IN_LOOP=yes 
goto del

:exit
set /p input=完成，按任意键退出。