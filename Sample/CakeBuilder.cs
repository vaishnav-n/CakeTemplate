﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.Tools.DotNetCore.VSTest;
using Cake.Common.Tools.GitVersion;
using Cake.Common.Tools.MSBuild;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Core;
using Cake.Core.Annotations;
using Newtonsoft.Json;
using Cake.ArgumentHelpers;
using Cake.Common;
using Cake.Core.IO;
using System.IO;


namespace Sample
{
    public static class CakeBuilder
    {
        static string BuildNumber = string.Empty;
        static string BranchName = string.Empty;


        //[CakeMethodAlias]
        //public static void TestingBuild(this ICakeContext context)
        //{
        //    context.MSBuild("./src/CakeDemo.sln");
        //}


        [CakeMethodAlias]
        public static void TaskBuild(this ICakeContext context, string solutionfilepath,string buildoutputpath )
        {
           // Version(context);
           // RestorePackages(context);

           // var path = context.MakeAbsolute(context.File("./ENT.UI/Properties/PublishProfiles/FolderProfile.pubxml"));

            var msBuildSettings = new MSBuildSettings()
                .SetConfiguration("Release")
                .WithProperty("DeployOnBuild", "true")
                .WithProperty("OutDir", buildoutputpath);
               // .WithProperty("PublishProfile", path.ToString());


            context.MSBuild(solutionfilepath, msBuildSettings);


        }

        [CakeMethodAlias]
        public static string Version(this ICakeContext context)
        {
            StringBuilder Message = new StringBuilder();
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
            Message.Append("AssemblySemVer: " + versionResult.AssemblySemVer + "\n");
            // Convert 12.1.3.4 to 1201030004 etc.
            string paddedVersionNumber = string.Join("", versionResult.AssemblySemVer.Split('.').Select(s => s.PadLeft(2, '0')).ToArray()) + "00";

            //Information("PaddedVersionNumber: " + paddedVersionNumber);

            BuildNumber = versionResult.SemVer;
            BranchName = versionResult.BranchName;

            Message.Append("BranchName updated: " + BranchName + "\n");
            Message.Append("BuildNumber updated: " + BuildNumber + "\n");
            //Information("BuildNumber updated: " + BuildNumber);
            return Message.ToString();
        }

        
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

            //settings.ToolPath = "c:\\tools\\gitversion\\gitversion.exe";

            settings.ToolPath = "C:\\Users\\vaishnavn\\.nuget\\packages\\gitversion.commandline\\5.2.4\\tools\\gitversion.exe";
            //}
        }


        [CakeMethodAlias]
        public static void PublishMultipleTasks(ICakeContext context, string jsonPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(jsonPath))
                {
                    ProcessPath lstprocessPath = JsonConvert.DeserializeObject<ProcessPath>(jsonPath);

                    if (lstprocessPath != null)
                    {
                        foreach (var path in lstprocessPath.lstPaths)
                        {
                            context.DeleteFiles(path.Deletepath);



                            string publishDirectory = path.PublishPath;



                            var publishSettings = new DotNetCorePublishSettings
                            {
                                Configuration = "Release",
                                OutputDirectory = publishDirectory,
                                Runtime = "win-x64"
                            };



                            context.DotNetCorePublish(path.CSprojPath, publishSettings);



                            var nuGetPackSettings = new NuGetPackSettings
                            {
                                OutputDirectory = "./publishpackage/",
                                Version = BuildNumber
                            };



                            context.NuGetPack(path.nuspecPath, nuGetPackSettings);
                        }
                    }
                }
            }
            catch (Exception ex)
            {



            }



        }
    }
}
