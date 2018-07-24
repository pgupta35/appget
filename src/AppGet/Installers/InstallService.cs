﻿using System.Collections.Generic;
using System.Linq;
using AppGet.Commands.Install;
using AppGet.FileTransfer;
using AppGet.HostSystem;
using AppGet.Manifest;
using AppGet.Manifests;
using NLog;

namespace AppGet.Installers
{
    public interface IInstallService
    {
        void Install(PackageManifest packageManifest, InstallOptions installOptions);
    }

    public class InstallService : IInstallService
    {
        private readonly Logger _logger;
        private readonly IFindInstaller _findInstaller;
        private readonly IPathResolver _pathResolver;
        private readonly IFileTransferService _fileTransferService;
        private readonly List<IInstallerWhisperer> _installWhisperers;

        public InstallService(Logger logger, IFindInstaller findInstaller, IPathResolver pathResolver,
            IFileTransferService fileTransferService, List<IInstallerWhisperer> installWhisperers)
        {
            _logger = logger;
            _findInstaller = findInstaller;
            _pathResolver = pathResolver;
            _fileTransferService = fileTransferService;
            _installWhisperers = installWhisperers;
        }

        public void Install(PackageManifest packageManifest, InstallOptions installOptions)
        {
            _logger.Info("Beginning installation of '{0}'", packageManifest.Name ?? packageManifest.Id);

            var whisperer = _installWhisperers.Single(c => c.CanHandle(packageManifest.InstallMethod));

            var installer = _findInstaller.GetBestInstaller(packageManifest.Installers);
            var installerPath = _fileTransferService.TransferFile(installer.Location, _pathResolver.TempFolder, installer.Sha256);

            whisperer.Install(installerPath, packageManifest, installOptions);

            _logger.Info("Installation completed for '{0}'", packageManifest.Name ?? packageManifest.Id);
        }
    }
}