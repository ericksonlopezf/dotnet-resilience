// Copyright © Erickson Lopez. MIT License.
const fs = require('fs');
const path = require('path');

function findXml(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      const found = findXml(full);
      if (found) return found;
    } else if (entry.name.endsWith('.xml')) {
      return full;
    }
  }
  return null;
}

const xmlPath = findXml('tests/EricksonLopez.Resilience.Polly.Tests/TestResults');
if (!xmlPath) {
  console.error('No XML coverage file found');
  process.exit(1);
}

const xml = fs.readFileSync(xmlPath, 'utf8');
const lines = xml.split('\n');
let inPolly = false;
let currentClass = '';

for (const line of lines) {
  if (line.includes('<package name="EricksonLopez.Resilience.Polly"')) {
    inPolly = true;
  } else if (inPolly && line.includes('</package>')) {
    inPolly = false;
  }
  if (inPolly) {
    const classMatch = line.match(/<class name="([^"]+)" filename="([^"]+)"/);
    if (classMatch) {
      currentClass = classMatch[1] + ' (' + classMatch[2] + ')';
    }
    if (line.includes('condition-coverage=') && !line.includes('100%')) {
      console.log(currentClass, '-->', line.trim());
    }
  }
}
