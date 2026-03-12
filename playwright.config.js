// @ts-check
const { defineConfig } = require('@playwright/test');

module.exports = defineConfig({
  testDir: './tests',
  use: {
    browserName: 'chromium',
    headless: true,
  },
  reporter: 'list',
});
