// Copyright © Erickson Lopez. MIT License.
const fs = require('fs');
const path = require('path');

const reportDir = 'D:/DevData/ericksonlopez.dev/dotnet-resilience/StrykerOutput';
const runs = fs.readdirSync(reportDir).filter(f => fs.statSync(path.join(reportDir, f)).isDirectory()).sort().reverse();
const latestJson = path.join(reportDir, runs[0], 'reports/mutation-report.json');

const data = JSON.parse(fs.readFileSync(latestJson, 'utf8'));

for (const [filePath, fileData] of Object.entries(data.files)) {
  const issues = fileData.mutants.filter(m => m.status === 'Survived' || m.status === 'NoCoverage');
  if (issues.length === 0) continue;

  console.log(`\n======================================================`);
  console.log(`FILE: ${filePath} (${issues.length} issues)`);
  console.log(`======================================================`);

  for (const m of issues) {
    console.log(`[${m.status}] Line ${m.location.start.line}:${m.location.start.column} | ID: ${m.id} | Mutator: ${m.mutatorName}`);
    console.log(`  Replacement: ${m.replacement}`);
    console.log(`  Description: ${m.description}`);
  }
}
