// Copyright © Erickson Lopez. MIT License.
const fs = require('fs');
const path = require('path');

function findCoberturaFiles(dir) {
    let results = [];
    const list = fs.readdirSync(dir, { withFileTypes: true });
    for (const item of list) {
        const fullPath = path.join(dir, item.name);
        if (item.isDirectory()) {
            results = results.concat(findCoberturaFiles(fullPath));
        } else if (item.name === 'coverage.cobertura.xml') {
            results.push(fullPath);
        }
    }
    return results;
}

const targetDir = process.argv[2] || 'tests';
const pkgFilter = process.argv[3];
const files = findCoberturaFiles(targetDir);

if (files.length === 0) {
    console.log('No coverage.cobertura.xml files found.');
    process.exit(1);
}

for (const file of files) {
    console.log('\n--- Coverage File: ' + file + ' ---');
    const content = fs.readFileSync(file, 'utf8');
    
    // Find packages
    const pkgBlocks = content.split('<package ');
    for (let i = 1; i < pkgBlocks.length; i++) {
        const block = pkgBlocks[i];
        const nameMatch = block.match(/name="([^"]+)"/);
        const lineMatch = block.match(/line-rate="([^"]+)"/);
        const branchMatch = block.match(/branch-rate="([^"]+)"/);
        
        const pkgName = nameMatch ? nameMatch[1] : 'unknown';
        const isMatch = pkgFilter ? (pkgFilter.startsWith('^') ? pkgName === pkgFilter.substring(1) : pkgName.includes(pkgFilter)) : true;
        if (!isMatch) {
            continue;
        }

        const lineRate = lineMatch ? (parseFloat(lineMatch[1]) * 100).toFixed(2) : '0';
        const branchRate = branchMatch ? (parseFloat(branchMatch[1]) * 100).toFixed(2) : '0';
        
        console.log(`Package: ${pkgName} | Line: ${lineRate}% | Branch: ${branchRate}%`);
        
        if (pkgName.startsWith('EricksonLopez.Resilience') && lineRate !== '100.00') {
            // Find uncovered classes and lines
            const classBlocks = block.split('<class ');
            for (let j = 1; j < classBlocks.length; j++) {
                const cBlock = classBlocks[j];
                const cName = cBlock.match(/name="([^"]+)"/)?.[1];
                const cLineRate = cBlock.match(/line-rate="([^"]+)"/)?.[1];
                if (cLineRate && parseFloat(cLineRate) < 1.0) {
                    console.log(`  Class: ${cName} (Line: ${(parseFloat(cLineRate)*100).toFixed(2)}%)`);
                    const lineMatches = cBlock.matchAll(/<line number="(\d+)" hits="0"/g);
                    for (const lm of lineMatches) {
                        console.log(`    Uncovered line: ${lm[1]}`);
                    }
                }
            }
        }
    }
}
