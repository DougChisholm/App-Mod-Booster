![Header image](https://github.com/DougChisholm/App-Mod-Assist/blob/main/repo-header.png)

# App-Mod-Booster
A project to show how GitHub coding agent can turn screenshots of legacy apps into working proof-of-concepts for cloud native Azure replacements if the legacy database schema is also provided

1. Fork this repo then open the coding agent and use app-mod-assist agent telling it "modernise my app" - making sure to replace the screenshots and sql schema first
2. Clone repo when code is generated locally and open VS Code
3. In terminal AZ LOGIN > Set a subscription context
4. Run the deploy.sh file (ensuring the settings in the bicep files are what you want - it will have RG name, SKU, UKSOUTH etc already set)
