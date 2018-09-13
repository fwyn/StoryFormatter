@Echo Off
PushD %~dp0

..\Release\StoryFormatter.exe %~n0.story

PopD
