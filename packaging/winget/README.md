# Manual WinGet publishing

The manifests in this directory are intentionally published without GitHub Actions.

## First submission

1. Build and publish a versioned GitHub Release installer.
2. Update the version, installer URL, SHA-256, release date, license URL, and release notes URL in a new version directory.
3. Validate the manifests:

   ```powershell
   winget validate .\packaging\winget\<version>
   ```

4. Test the installer and uninstaller on a clean Windows Sandbox or VM.
5. Sign in to WinGetCreate without putting a token on the command line:

   ```powershell
   wingetcreate token -s
   ```

6. Submit the manifest directory:

   ```powershell
   wingetcreate submit --prtitle "New package: Archura.Windrop version <version>" .\packaging\winget\<version>
   ```

## Later releases

Create a new version directory; never overwrite a released installer asset after its manifest has been submitted. After validation and clean-machine testing, submit the new directory with:

```powershell
wingetcreate submit --prtitle "Update: Archura.Windrop version <version>" .\packaging\winget\<version>
```
