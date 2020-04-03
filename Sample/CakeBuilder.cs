using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cake.Common.Tools.GitVersion;
using Cake.Common.Tools.MSBuild;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.ArgumentHelpers;
using Cake.Common;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.IO;
using Cake.Common.Diagnostics;
using Cake.Npm.Install;
using Cake.Npm;
using Cake.Npm.RunScript;
using Cake.Core.IO;

namespace Sample
{
    public static class CakeBuilder
    {
        static string OctopusDeployUrl = string.Empty;
        static string Target = string.Empty;
        static string BuildNumber = string.Empty;
        static string MsBuildLogger = string.Empty;
        static string OctopusDeployApiKey = string.Empty;
        static string ProjectName = string.Empty;
        static string ProcessesProjectName = string.Empty;
        static string APIProjectName = string.Empty;
        static string DeploymentBranches = string.Empty;
        static string TeamCityBuildAgentDirectory = string.Empty;
        static string NodeBackupPath = string.Empty;
        static string BranchName = string.Empty;
        static string tenant = string.Empty;
        static string solfilepath=string.Empty;


        [CakeMethodAlias]
        public static void TestingBuild(this ICakeContext context)
        {
            context.MSBuild("./src/CakeDemo.sln");
        }

        [CakeMethodAlias]
        public static void buildTasks(this ICakeContext context)
        {
           // OctopusDeployUrl = "https://deploy.hhaexchange.com";
            //Target = context.Argument("target", "OctoRelease");
            BuildNumber = context.ArgumentOrEnvironmentVariable("build.number", "", "0.0.1-local.0");
           // OctopusDeployApiKey = context.ArgumentOrEnvironmentVariable("OctopusDeployApiKey", "");
            MsBuildLogger = context.ArgumentOrEnvironmentVariable("MsBuildLogger", "", "");
          //  ProjectName = context.ArgumentOrEnvironmentVariable("ProjectName", "", "ENT");
          //  ProcessesProjectName = context.ArgumentOrEnvironmentVariable("ProcessesProjectName", "", "ENT Processes");
          //  APIProjectName = context.ArgumentOrEnvironmentVariable("APIProjectName", "", "ENT.Internal.API");
            DeploymentBranches = context.ArgumentOrEnvironmentVariable("DeploymentBranches", "", " ");
            TeamCityBuildAgentDirectory = context.ArgumentOrEnvironmentVariable("teamcity.agent.home.dir", "", "c:\\BuildAgent");
            NodeBackupPath = TeamCityBuildAgentDirectory + "\\node_backup\\ENTP\\node_modules";
            BranchName = null;
            tenant = null;


            TaskBuild(context);
            
        }

        [CakeMethodAlias]
        public static void TaskBuild(ICakeContext context)
        {
           // Version(context);
           // RestorePackages(context);

            var path = context.MakeAbsolute(context.File("./ENT.UI/Properties/PublishProfiles/FolderProfile.pubxml"));

            var msBuildSettings = new MSBuildSettings()
                .SetConfiguration("Release")
                .WithProperty("DeployOnBuild", "true")
                .WithProperty("PublishProfile", path.ToString());

            if (!string.IsNullOrEmpty(MsBuildLogger))
            {
                msBuildSettings.ArgumentCustomization = arguments =>
                        arguments.Append(string.Format("/logger:{0}", MsBuildLogger));
            }

            context.MSBuild(solfilepath, msBuildSettings);

            //var npmRunSettings = new NpmRunScriptSettings()
            //{
            //    ScriptName = "build-prod",
            //    WorkingDirectory = "ENT.UI/",

            //};

            //context.NpmRunScript(npmRunSettings);

            ////Information("Backup node modules folder to " + NodeBackupPath);

            //if (context.DirectoryExists("ENT.UI/node_modules"))
            //{
            //    if (context.DirectoryExists(NodeBackupPath))
            //    {
            //        //Information("Delete existing node modules backup");

            //        context.DeleteDirectory(NodeBackupPath,
            //            new DeleteDirectorySettings
            //            {
            //                Recursive = true
            //            });
            //    }

            //    //Information("Move working node modules to backup folder");
            //    try
            //    {
            //        context.MoveDirectory("ENT.UI/node_modules", NodeBackupPath);
            //    }
            //    catch (Exception ex)
            //    {
            //        context.Warning("Failed to move node modules to backup " + ex.ToString());
            //    }
            //}
        }

        [CakeMethodAlias]
        public static void Version(ICakeContext context)
        {
            GitVersionSettings buildServerSettings = new GitVersionSettings
            {
                OutputType = GitVersionOutput.BuildServer,
                UpdateAssemblyInfo = true
            };

            SetGitVersionPath(buildServerSettings);

            // Ran twice because the result is empty when using buildserver mode but we need to output to TeamCity
            // and use the result
            context.GitVersion(buildServerSettings);

            GitVersionSettings localSettings = new GitVersionSettings();

            SetGitVersionPath(localSettings);

            var versionResult = context.GitVersion(localSettings);

            //Information("AssemblySemVer: " + versionResult.AssemblySemVer);

            // Convert 12.1.3.4 to 1201030004 etc.
            string paddedVersionNumber = string.Join("", versionResult.AssemblySemVer.Split('.').Select(s => s.PadLeft(2, '0')).ToArray()) + "00";

            //Information("PaddedVersionNumber: " + paddedVersionNumber);

            BuildNumber = versionResult.SemVer;
            BranchName = versionResult.BranchName;

            //Information("BuildNumber updated: " + BuildNumber);
        }

        //public static void RestorePackages(ICakeContext context)
        //{
        //    context.NuGetRestore("ENT.sln");

        //    context.DotNetCoreRestore("ENT.sln");

        //    //Information("Restore node modules folder");

        //    if (context.DirectoryExists(NodeBackupPath))
        //    {
        //        if (context.DirectoryExists("./ENT.UI/node_modules"))
        //        {
        //            //Information("Delete existing node modules folder");

        //            context.DeleteDirectory("./ENT.UI/node_modules",
        //                new DeleteDirectorySettings
        //                {
        //                    Recursive = true
        //                });
        //        }

        //        //Information("Move backup node modules folder to working directory");

        //        try
        //        {
        //            context.MoveDirectory(NodeBackupPath, "./ENT.UI/node_modules");
        //        }
        //        catch (Exception ex)
        //        {
        //            context.Warning("Failed to move node backup into project" + ex.ToString());
        //        }
        //    }

        //    var npmInstallSettings = new NpmInstallSettings()
        //    {
        //        LogLevel = NpmLogLevel.Verbose,
        //        WorkingDirectory = "ENT.UI/"
        //    };

        //    context.NpmInstall(npmInstallSettings);
        //}

        public static bool IsFeatureBranchWithTenant(ICakeContext context, string DeploymentBranches, string BranchName, string tenant)
        {
            //	Information("Deployment Branches are: " + DeploymentBranches);
            //Information("Current Branch is: " + BranchName);

            if (!string.IsNullOrEmpty(DeploymentBranches))
            {
                var deploymentBranches = DeploymentBranches.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

                foreach (string deploymentBranch in deploymentBranches)
                {
                    //Information("Checking if branch is:" + deploymentBranch);

                    if (BranchName.ToLower() == deploymentBranch.Trim().ToLower())
                    {
                        var pattern = "([^/]*)([/]*)([^-_]+(?:-|_)[^-_]+)([-|_])(.*)";

                        System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                        var match = r.Match(BranchName);

                        if (match.Success)
                        {
                            var currentBranchFolder = match.Groups[1].Value;
                            var currentTicketNumber = match.Groups[3].Value;
                            var currentBranch = match.Groups[5].Value;

                            //Information("Folder: " + currentBranchFolder);
                            //Information("Ticket: " + currentTicketNumber);
                            //Information("Branch: " + currentBranch);

                            tenant = currentTicketNumber;

                            //Information("Using tenant:" + tenant);

                            return true;
                        }
                    }
                }

                return false;
            }

            return false;
        }
        
        public static void SetGitVersionPath(GitVersionSettings settings)
        {
            //if (Cake.Common.Build.TeamCity.IsRunningOnTeamCity)
            //{
            //Information("Using shared GitVersion");

            settings.ToolPath = "c:\\tools\\gitversion\\gitversion.exe";
            //}
        }
    }
}
