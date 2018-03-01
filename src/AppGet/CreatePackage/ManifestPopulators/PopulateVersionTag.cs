﻿using System.Diagnostics;
using AppGet.CommandLine.Prompts;
using AppGet.Manifests;

namespace AppGet.CreatePackage.ManifestPopulators
{
    public class PopulateVersionTag : IPopulateManifest
    {
        private readonly TextPrompt _prompt;
        private const string LATEST = "latest";

        public PopulateVersionTag(TextPrompt prompt)
        {
            _prompt = prompt;
        }

        public void Populate(PackageManifest manifest, FileVersionInfo fileVersionInfo, bool interactive)
        {
            var tag = _prompt.Request("Version Tag", LATEST, interactive).ToLowerInvariant();

            if (tag == LATEST)
            {
                tag = null;
            }

            manifest.VersionTag = tag;
        }
    }
}