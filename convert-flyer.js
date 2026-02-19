const puppeteer = require('puppeteer-core');
const path = require('path');

const inputFile = process.argv[2] || 'flyer-print.html';
const outputFile = process.argv[3] || inputFile.replace('.html', '.png');

(async () => {
  const browser = await puppeteer.launch({
    executablePath: '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  const page = await browser.newPage();

  // A5 = 148 x 210 mm. At 300 DPI: 1748 x 2480 px
  // Viewport = 874 x 1240, deviceScaleFactor = 2 → output = 1748 x 2480
  await page.setViewport({
    width: 874,
    height: 1240,
    deviceScaleFactor: 2
  });

  const htmlPath = path.resolve(__dirname, 'wwwroot', inputFile);
  await page.goto(`file://${htmlPath}`, { waitUntil: 'networkidle0', timeout: 30000 });

  // Wait for fonts
  await page.evaluate(() => document.fonts.ready);
  await new Promise(r => setTimeout(r, 2500));

  const outputPath = path.resolve(__dirname, 'wwwroot', outputFile);
  await page.screenshot({
    path: outputPath,
    type: 'png',
    clip: { x: 0, y: 0, width: 874, height: 1240 }
  });

  console.log(`Flyer saved to: ${outputPath}`);
  console.log('Size: A5 (148 x 210 mm) at 300 DPI = 1748 x 2480 px');

  await browser.close();
})();
