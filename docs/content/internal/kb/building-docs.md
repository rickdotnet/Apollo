# Building Doc Site

This outlines the steps to build the documentation site locally.

## Install

In order to run the docs locally, you must have Docfx installed. Unfortunately, it also requires `Node.js` to be installed as well.

### Docfx Reference

See: [docfx](https://dotnet.github.io/docfx/index.html)

### Ubuntu Based (popOS) Install

**Install Node.js** (See: [Node.js](https://nodejs.org/en/download/package-manager))
```shell
# installs nvm (Node Version Manager)
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.0/install.sh | bash

# download and install Node.js (you may need to restart the terminal)
nvm install 22

# verifies the right Node.js version is in the environment
node -v # should print `v22.12.0`

# verifies the right npm version is in the environment
npm -v # should print `10.9.0`
```

**Install Docfx**
```shell
dotnet tool update -g docfx
```

> [!NOTE]
> Make sure you restart your terminal if the following commands do not work.

## Build and Serve the Docs

**Navigate to the Docs Directory**
```shell
cd docs
```

**Build the Docs** *(optional)*
```shell
docfx docfx.json
````

**Serve the Docs**
```shell
docfx docfx.json --serve
```

## Notes

The `docfx.json` file is the configuration file for the documentation site. Projects added to the `src` directory will be included in the `Source` documentation site.

**docfx.json**
[!code-json[](../../../docfx.json)]