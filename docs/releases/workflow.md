# Release workflow

The **Release** workflow is manually triggered. It validates the version, runs checks, builds
both framework targets and attaches verified archives to a draft release by default.

## Run a build

1. Open the repository's **Actions** tab and select **Release**.
2. Choose **Run workflow** and the source branch.
3. Enter a version such as `0.1.0`.
4. Keep draft mode enabled while reviewing the artifacts.

The workflow uses the selected commit consistently across jobs. An existing tag or release
version is rejected instead of silently replacing earlier artifacts. Versions containing a
prerelease suffix are marked as prereleases.

## Check the artifacts

Confirm the manifest's version, commit and framework. Archives contain only the
owned library, its API XML and deployment information. The server's IOM and framework files
are excluded. Verify archive hashes with `SHA256SUMS.txt`.

## Preview documentation

The **Documentation** workflow builds a static preview artifact. Its default operation does
not publish a website. The site uses the repository path `/Schema/`. Download the artifact
into a matching directory and serve its parent:

```sh
gh run download RUN_ID --repo InnovatorStack/Schema \
    --name documentation-preview --dir preview/Schema
python3 -m http.server 8000 --directory preview
```

Open `http://localhost:8000/Schema/`. Replace `RUN_ID` with the Documentation run's ID.

Pages publication is a separate manual option and is blocked while the repository is private:
ordinary GitHub Pages can expose a site publicly even when its repository is private.
After a deliberate public launch, select **GitHub Actions** as the publishing source in
**Settings → Pages** before using that option.

## Release visibility

The workflow does not change repository visibility.
