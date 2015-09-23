
@SET GENIUSXWORKFLOWDIR=".\GeniusX.AXA.Workflows\bin"

IF NOT EXIST %GENIUSXWORKFLOWDIR%\NUL MD %GENIUSXWORKFLOWDIR%
copy "..\Lib\EnterpriseLibrary\Bin\Microsoft.Practices.Unity.dll" %GENIUSXWORKFLOWDIR% /y /b /v
copy "..\Lib\EnterpriseLibrary\Bin\Microsoft.Practices.Unity.Configuration.dll" %GENIUSXWORKFLOWDIR% /y /b /v
copy ".\Solution Items\K2Processes.K2SmartObjectService.Core.dll" %GENIUSXWORKFLOWDIR% /y /b /v
copy ".\Solution Items\Xiap.K2.Framework.CustomWorklistClient.dll" %GENIUSXWORKFLOWDIR% /y /b /v
