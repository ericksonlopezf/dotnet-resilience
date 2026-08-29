// Copyright © Erickson Lopez. MIT License.
const fs = require('fs');
const path = require('path');

const reportPath = 'StrykerOutput/2026-08-27.23-05-50/reports/mutation-report.json';
const report = JSON.parse(fs.readFileSync(reportPath, 'utf8'));

console.log('=== SUMMARY OF SURVIVED & NO COVERAGE MUTANTS ===\n');

for (const [file, fileData] of Object.entries(report.files)) {
  const survivedOrNoCov = fileData.mutants.filter(m => m.status === 'Survived' || m.status === 'NoCoverage');
  if (survivedOrNoCov.length > 0) {
    console.log(`\nFile: ${file} (${survivedOrNoCov.length} issues)`);
    for (const m of survivedOrNoCov) {
      console.log(`  [${m.status}] Line ${m.location.start.line}:${m.location.start.column} | Mutator: ${m.mutatorName}`);
      console.log(`    Replacement: ${m.replacement}`);
      console.log(`    Description: ${m.description}`);
    }
  }
}
