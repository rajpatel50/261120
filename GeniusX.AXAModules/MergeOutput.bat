set echo off
SET SourceFolder=.
SET TargetFolder=..\..\..\..\XIAP Framework\6_0_129\root

xcopy %SourceFolder%\Config\Custom\*.* "%TargetFolder%\XIAP\Client Services\Xiap.ClientServices\Config\Custom\AXA\" /i /d /y /r
xcopy %SourceFolder%\Config\Custom\*.* "%TargetFolder%\XIAP\ServiceHost\XiapServiceHost\Config\Custom\AXA\" /i /d /y /r

xcopy %SourceFolder%\Claims\GeniusX.AXA.Claims.BusinessLogic\bin\debug\GeniusX.AXA.Claims.BusinessLogic*.* "%TargetFolder%\XIAP\ServiceHost\XiapServiceHost\bin\" /i /d /y /r
xcopy %SourceFolder%\Claims\GeniusX.AXA.Claims.BusinessLogic\bin\debug\GeniusX.AXA.Claims.BusinessLogic*.* "%TargetFolder%\XIAP\Client Services\Xiap.ClientServices\bin\" /i /d /y /r
xcopy %SourceFolder%\InsuranceDirectory\GeniusX.AXA.InsuranceDirectory.BusinessLogic\bin\debug\GeniusX.AXA.InsuranceDirectory.BusinessLogic*.* "%TargetFolder%\XIAP\ServiceHost\XiapServiceHost\bin\" /i /d /y /r
xcopy %SourceFolder%\InsuranceDirectory\GeniusX.AXA.InsuranceDirectory.BusinessLogic\bin\debug\GeniusX.AXA.InsuranceDirectory.BusinessLogic*.* "%TargetFolder%\XIAP\Client Services\Xiap.ClientServices\bin\" /i /d /y /r
xcopy %SourceFolder%\Underwriting\GeniusX.AXA.Underwriting.BusinessLogic\bin\debug\GeniusX.AXA.Underwriting.BusinessLogic*.* "%TargetFolder%\XIAP\ServiceHost\XiapServiceHost\bin\" /i /d /y /r
xcopy %SourceFolder%\Underwriting\GeniusX.AXA.Underwriting.BusinessLogic\bin\debug\GeniusX.AXA.Underwriting.BusinessLogic*.* "%TargetFolder%\XIAP\Client Services\Xiap.ClientServices\bin\" /i /d /y /r
xcopy %SourceFolder%\"Design Studio"\GeniusX.AXA.DesignStudio.Framework\bin\Debug\GeniusX.AXA.DesignStudio.Framework*.* "%TargetFolder%\XIAP.DesignStudio\bin\Debug\" /i /d /y /r
xcopy %SourceFolder%\DPService\GeniusX.AXA.DPService\bin\Debug\GeniusX.AXA.DPService*.* "%TargetFolder%\XIAP\ServiceHost\XiapServiceHost\bin\" /i /d /y /r
xcopy %SourceFolder%\DPService\GeniusX.AXA.DPService\bin\Debug\GeniusX.AXA.DPService*.* "%TargetFolder%\XIAP\Client Services\Xiap.ClientServices\bin\" /i /d /y /r


del "%TargetFolder%\XIAP.Shell\XIAP.Shell.Web\ClientBin\GeniusX.AXA.FrontendModules.Claims.xap"
del "%TargetFolder%\XIAP.Shell\XIAP.Shell.Web\ClientBin\GeniusX.AXA.FrontendModules.Underwriting.xap"
del "%TargetFolder%\XIAP.Shell\XIAP.Shell.Web\ClientBin\GeniusX.AXA.FrontendModules.InsuranceDirectory.xap"
del "%TargetFolder%\XIAP.Shell\XIAP.Shell.Web\ClientBin\GeniusX.AXA.FrontendModules.Search.xap"

xcopy %SourceFolder%\Claims\GeniusX.AXA.FrontendModules.Claims\Bin\debug\GeniusX.AXA.FrontendModules.Claims.xap "%TargetFolder%\XIAP.Shell\XIAP.Shell.Web\ClientBin\" /i /d /y /r
xcopy %SourceFolder%\Claims\GeniusX.AXA.FrontendModules.Claims\Bin\debug\GeniusX.AXA.FrontendModules.Claims.* "%TargetFolder%\XIAP.Shell\XIAP.Shell.Web\bin\" /i /d /y /r

xcopy %SourceFolder%\Underwriting\GeniusX.AXA.FrontendModules.Underwriting\Bin\debug\GeniusX.AXA.FrontendModules.Underwriting.xap "%TargetFolder%\XIAP.Shell\XIAP.Shell.Web\ClientBin\" /i /d /y /r
xcopy %SourceFolder%\InsuranceDirectory\GeniusX.AXA.FrontendModules.InsuranceDirectory\Bin\Debug\GeniusX.AXA.FrontendModules.InsuranceDirectory.xap "%TargetFolder%\XIAP.Shell\XIAP.Shell.Web\ClientBin\" /i /d /y /r
xcopy %SourceFolder%\Search\GeniusX.AXA.FrontendModules.Search\Bin\Debug\GeniusX.AXA.FrontendModules.Search.xap "%TargetFolder%\XIAP.Shell\XIAP.Shell.Web\ClientBin\" /i /d /y /r

xcopy %SourceFolder%\GeniusX.AXA.CustomUI\bin\*.* "%TargetFolder%\XIAP\Client Services\Xiap.ClientServices\bin\" /i /d /y /r
xcopy %SourceFolder%\GeniusX.AXA.CustomUI\CustomUI\*.aspx "%TargetFolder%\XIAP\Client Services\Xiap.ClientServices\CustomUI\" /i /d /y /r

del %TargetFolder%\XIAP.Shell\XIAP.Shell.Web\ShellConfiguration.AXA.xaml /q /f
copy "%SourceFolder%\Config\Shell UI\ShellConfiguration.AXA.xaml" "%TargetFolder%\XIAP.Shell\XIAP.Shell.Web\" /d /y

rem 'It may not be advisable to always move/overrite the web config during merge. Uncomment below if needed.
rem move "%TargetFolder%\XIAP\Client Services\Xiap.ClientServices\Web.Config" "%TargetFolder%\XIAP\Client Services\Xiap.ClientServices\Web.core.Config"
rem copy "%SourceFolder%\Config\Client Services\Web.ClientServices.AXA.config" "%TargetFolder%\XIAP\Client Services\Xiap.ClientServices\Web.Config" /d /y
rem move "%TargetFolder%\XIAP\ServiceHost\XiapServiceHost\Web.Config" "%TargetFolder%\XIAP\ServiceHost\XiapServiceHost\Web.core.Config"
rem copy "%SourceFolder%\Config\Service Host\Web.ServiceHost.AXA.config" "%TargetFolder%\XIAP\ServiceHost\XiapServiceHost\Web.Config" /d /y

xcopy .\Config\Custom\*.* "ClaimWakeUpService\Xiap.DataMigration.GeniusInterface.AXACS.Service\Config\Custom\AXA" /i /d /y /r /e
