# GitHub Pages Setup for SideSnap

This guide explains how to deploy the SideSnap website and privacy policy to GitHub Pages.

## Quick Setup

### Option 1: Using docs/ Folder (Recommended)

1. **Go to your GitHub repository settings**:
   - Navigate to: `https://github.com/dev-alt/SideSnap/settings/pages`

2. **Configure GitHub Pages**:
   - **Source**: Deploy from a branch
   - **Branch**: `master` (or `main`)
   - **Folder**: `/docs`
   - Click **Save**

3. **Wait for deployment** (1-2 minutes)

4. **Access your site**:
   - **Website**: `https://dev-alt.github.io/SideSnap/`
   - **Privacy Policy**: `https://dev-alt.github.io/SideSnap/privacy-policy.html`

### Option 2: Using gh-pages Branch

If you prefer a separate branch:

```bash
# Create gh-pages branch
git checkout --orphan gh-pages

# Copy docs files to root
cp docs/index.html .
cp docs/privacy-policy.html .

# Remove other files
git rm -rf --cached .
git add index.html privacy-policy.html

# Commit and push
git commit -m "Deploy to GitHub Pages"
git push origin gh-pages

# Switch back to master
git checkout master
```

Then configure in repository settings to use `gh-pages` branch.

## Files Deployed

- **index.html** - Landing page with features, download links, and information
- **privacy-policy.html** - Privacy policy required for Microsoft Store submission

## Privacy Policy URL

Once deployed, use this URL in your Microsoft Store submission:

```
https://dev-alt.github.io/SideSnap/privacy-policy.html
```

## Updating the Site

### Using docs/ Folder

1. Edit files in `docs/` directory
2. Commit and push to master:
   ```bash
   git add docs/
   git commit -m "Update website"
   git push origin master
   ```
3. GitHub Pages will automatically redeploy (1-2 minutes)

### Using gh-pages Branch

1. Checkout gh-pages branch:
   ```bash
   git checkout gh-pages
   ```
2. Make changes to HTML files
3. Commit and push:
   ```bash
   git add .
   git commit -m "Update website"
   git push origin gh-pages
   ```
4. Switch back to master:
   ```bash
   git checkout master
   ```

## Custom Domain (Optional)

If you want to use a custom domain like `sidesnap.com`:

1. **Add CNAME file** in docs/ (or gh-pages root):
   ```
   sidesnap.com
   ```

2. **Configure DNS** with your domain provider:
   - Add CNAME record pointing to: `dev-alt.github.io`
   - Or use A records pointing to GitHub Pages IPs:
     - 185.199.108.153
     - 185.199.109.153
     - 185.199.110.153
     - 185.199.111.153

3. **Update repository settings**:
   - Settings → Pages → Custom domain
   - Enter your domain
   - Enable HTTPS

## Verification

After deployment, verify:

1. ✅ **Homepage loads**: Visit `https://dev-alt.github.io/SideSnap/`
2. ✅ **Privacy policy loads**: Visit `https://dev-alt.github.io/SideSnap/privacy-policy.html`
3. ✅ **Links work**: Click all navigation links
4. ✅ **Mobile responsive**: Test on mobile device
5. ✅ **HTTPS enabled**: URL shows padlock icon

## Updating Microsoft Store Submission

Once GitHub Pages is live, update your Store submission:

1. **Partner Center** → Your App → Store listings
2. **Privacy policy**: Enter `https://dev-alt.github.io/SideSnap/privacy-policy.html`
3. **Website** (optional): Enter `https://dev-alt.github.io/SideSnap/`
4. Save changes

## Troubleshooting

### Site Not Loading (404 Error)

- Check GitHub Pages settings are correct
- Verify branch and folder are set properly
- Wait 2-5 minutes for initial deployment
- Check repository is public (or you have GitHub Pro for private repos)

### Privacy Policy Not Found

- Verify file is named `privacy-policy.html` (lowercase, hyphen)
- Check file exists in correct folder (docs/ or root of gh-pages)
- Clear browser cache and try again

### Styles Not Loading

- Check CSS is inline in HTML files (it is)
- Verify no mixed content warnings (HTTP vs HTTPS)
- Open browser developer tools to check for errors

## Current Configuration

The SideSnap repository is configured to use:

- **Repository**: `dev-alt/SideSnap`
- **Branch**: `master`
- **Folder**: `/docs`
- **URLs**:
  - Homepage: `https://dev-alt.github.io/SideSnap/`
  - Privacy Policy: `https://dev-alt.github.io/SideSnap/privacy-policy.html`

## Next Steps

1. Enable GitHub Pages in repository settings (see Option 1 above)
2. Wait for deployment
3. Verify both pages load correctly
4. Update Microsoft Store submission with privacy policy URL
5. (Optional) Add custom domain

That's it! Your website and privacy policy are now live and ready for the Microsoft Store submission. 🚀
