﻿//            Copyright Keysight Technologies 2012-2019
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using OpenTap;
using OpenTap.Cli;

namespace OpenTap.Sdk.New
{
    [Display("vs", "Visual Studio project.", Groups: new[] { "sdk", "new", "integration" })]
    public class GenerateVs : GenerateType
    {
        public override int Execute(CancellationToken cancellationToken)
        {
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("OpenTap.Sdk.New.Resources.slnTemplate.txt")))
            {
                var ns = TryGetNamespace();
                var content = ReplaceInTemplate(reader.ReadToEnd(), ns);
                WriteFile(output ?? Path.Combine(Directory.GetCurrentDirectory(), ns + ".sln"), content);
            }

            return 0;
        }
    }

    [Display("gitlab-ci", "GitLab CI build script. For building and publishing the TapPackage in this project.", Groups: new[] { "sdk", "new", "integration" })]
    public class GenerateGitlab : GenerateType
    {
        public override int Execute(CancellationToken cancellationToken)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OpenTap.Sdk.New.Resources.gitlabCiTemplate.txt"))
            using (var reader = new StreamReader(stream))
            {
                var content = ReplaceInTemplate(reader.ReadToEnd(), TryGetNamespace());
                WriteFile(output ?? Path.Combine(Directory.GetCurrentDirectory(), ".gitlab-ci.yml"), content);
            }

            return 0;
        }
    }

    [Display("vscode", "Files to enable building and debugging with vscode.", Groups: new[] { "sdk", "new", "integration" })]
    public class GenerateVsCode : GenerateType
    {
        public override int Execute(CancellationToken cancellationToken)
        {
            var vsCodeDir = ".vscode";

            // create .vscode folder
            if (Directory.Exists(vsCodeDir) == false)
                Directory.CreateDirectory(vsCodeDir);

            // .tasks
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OpenTap.Sdk.New.Resources.tasksTemplate.txt"))
            using (var reader = new StreamReader(stream))
                WriteFile(output ?? Path.Combine(vsCodeDir, "tasks.json"), reader.ReadToEnd());

            // .launch
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OpenTap.Sdk.New.Resources.launchTemplate.txt"))
            using (var reader = new StreamReader(stream))
            {
                var content = ReplaceInTemplate(reader.ReadToEnd(), TryGetNamespace());
                var tapPlans = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.TapPlan", SearchOption.AllDirectories);
                if (tapPlans.Count() == 1)
                {
                    log.Info("Found one TapPlan. Using the plan for debugging.");
                    content = Regex.Replace(content, "<tap plan>", (m) =>
                    {
                        return Path.GetFileName(tapPlans.First());
                    });
                }
                else
                    log.Info("Please change <tap plan> in the '.vscode/launch.json' file.");

                WriteFile(output ?? Path.Combine(vsCodeDir, "launch.json"), content);
            }

            return 0;
        }
    }

    [Display("gitversion", "Configures automatic version of the package using version numbers generated from git history.", Groups: new[] { "sdk", "new", "integration" })]
    public class GenerateGit : GenerateType
    {
        public override int Execute(CancellationToken cancellationToken)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OpenTap.Sdk.New.Resources.GitVersionTemplate.txt"))
            using (var reader = new StreamReader(stream))
            {
                if (File.Exists("package.xml"))
                {
                    log.Info("Found 'package.xml' file. Do you want to update the version?");

                    var request = new OverrideRequest();
                    UserInput.Request(request, true);

                    if (request.Override == RequestEnum.Yes)
                    {
                        var text = File.ReadAllText("package.xml");
                        text = Regex.Replace(text, "(<Package.*?Version=\")(.*?)(\".*?>)", (m) =>
                        {
                            return $"{m.Groups[1].Value}$(GitVersion){m.Groups[3].Value}";
                        });
                        File.WriteAllText("package.xml", text);
                    }
                }
                WriteFile(output ?? Path.Combine(Directory.GetCurrentDirectory(), ".gitversion"), reader.ReadToEnd());
            }

            return 0;
        }
    }
}