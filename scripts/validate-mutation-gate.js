// Copyright © Erickson Lopez. MIT License.
const fs = require('fs');
const path = require('path');
const https = require('https');

function loadThresholds(configPath = 'stryker-config.json') {
  let thresholds = { high: 100, low: 98, break: 95 };
  try {
    if (fs.existsSync(configPath)) {
      const config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
      const t = config['stryker-config']?.thresholds || config.thresholds || {};
      thresholds = { high: t.high ?? 100, low: t.low ?? 98, break: t.break ?? 95 };
    }
  } catch (err) {
    console.warn(`Could not parse ${configPath}: ${err.message}`);
  }
  return thresholds;
}

function fetchCommitStatuses(sha) {
  const token = process.env.GITHUB_TOKEN || process.env.GH_TOKEN;
  const repo = process.env.GITHUB_REPOSITORY;
  const apiUrl = process.env.GITHUB_API_URL || 'https://api.github.com';

  if (!token || !repo || !sha || sha === 'unknown') {
    return Promise.resolve([]);
  }

  const parsedUrl = new URL(apiUrl);
  const path = `/repos/${repo}/commits/${sha}/statuses`;

  return new Promise((resolve) => {
    const req = https.request({
      hostname: parsedUrl.hostname,
      port: parsedUrl.port || 443,
      path: path,
      method: 'GET',
      headers: {
        'User-Agent': 'EricksonLopez-Stryker-Release-Gate',
        'Authorization': `Bearer ${token}`,
        'Accept': 'application/vnd.github+json'
      }
    }, (res) => {
      let body = '';
      res.on('data', chunk => { body += chunk; });
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          try {
            const data = JSON.parse(body);
            resolve(Array.isArray(data) ? data : []);
          } catch (e) {
            console.warn(`[ReleaseGate] Error parsing GitHub API response: ${e.message}`);
            resolve([]);
          }
        } else {
          console.warn(`[ReleaseGate] GitHub API statuses responded with ${res.statusCode}: ${body}`);
          resolve([]);
        }
      });
    });

    req.on('error', (err) => {
      console.warn(`[ReleaseGate] Failed to fetch commit statuses: ${err.message}`);
      resolve([]);
    });

    req.end();
  });
}

function writeStepSummary(markdown) {
  const stepSummaryPath = process.env.GITHUB_STEP_SUMMARY;
  if (stepSummaryPath) {
    fs.appendFileSync(stepSummaryPath, markdown + '\n');
  }
}

async function main() {
  const targetSha = process.argv[2] || process.env.GITHUB_SHA || 'unknown';
  const skipCheck = process.env.SKIP_MUTATION_CHECK === 'true' || process.argv[3] === '--skip';
  const thresholds = loadThresholds('stryker-config.json');

  console.log(`==================================================`);
  console.log(`  RELEASE MUTATION TESTING QUALITY GATE VALIDATOR `);
  console.log(`  Target Commit SHA: ${targetSha}`);
  console.log(`  Break Threshold:   ≥${thresholds.break}%`);
  console.log(`==================================================\n`);

  if (skipCheck) {
    console.log(`⚠️ Mutation check was explicitly bypassed via SKIP_MUTATION_CHECK.`);
    writeStepSummary(`
# 🛡️ Release Mutation Testing Quality Gate Verification

| Metric | Value |
|--------|-------|
| **Target Commit SHA** | \`${targetSha.substring(0, 7)}\` |
| **Quality Gate Status** | ⚠️ **BYPASS REQUESTED** |
| **Gate Verdict** | ✅ **RELEASE ALLOWED (MANUAL OVERRIDE)** |

> [!WARNING]
> Mutation quality gate check was bypassed via explicit user request (\`SKIP_MUTATION_CHECK=true\`).
`);
    process.exit(0);
  }

  // 1. Check local StrykerOutput metadata if available
  let localSummary = null;
  const localSummaryPath = path.join('StrykerOutput', 'mutation-summary.json');
  if (fs.existsSync(localSummaryPath)) {
    try {
      localSummary = JSON.parse(fs.readFileSync(localSummaryPath, 'utf8'));
    } catch (e) {
      console.warn(`Could not parse local summary: ${e.message}`);
    }
  }

  // 2. Check GitHub Commit Statuses
  const statuses = await fetchCommitStatuses(targetSha);
  const mainGateStatus = statuses.find(s => s.context === 'quality-gate/mutation-testing');

  let passed = false;
  let score = null;
  let executionDate = 'unknown';
  let runUrl = '';
  let statusText = '❌ FAILED';
  let detailsFound = false;

  if (mainGateStatus) {
    detailsFound = true;
    passed = mainGateStatus.state === 'success';
    executionDate = mainGateStatus.updated_at || mainGateStatus.created_at || new Date().toISOString();
    runUrl = mainGateStatus.target_url || '';
    const scoreMatch = (mainGateStatus.description || '').match(/Score:\s*([\d.]+)%/i);
    if (scoreMatch) {
      score = parseFloat(scoreMatch[1]);
    }
    statusText = passed ? (score !== null && score >= thresholds.high ? '✅ HIGH' : (score !== null && score >= thresholds.low ? '🟡 LOW' : '🟠 WARNING')) : '❌ FAILED';
  } else if (localSummary && (localSummary.commit_sha === targetSha || targetSha === 'unknown')) {
    detailsFound = true;
    passed = localSummary.passed_break === true;
    score = localSummary.overall_mutation_score;
    executionDate = localSummary.execution_date;
    runUrl = localSummary.run_url;
    statusText = localSummary.status;
  }

  if (detailsFound && passed) {
    console.log(`✅ Mutation Testing Quality Gate Passed!`);
    console.log(`   - Commit SHA:       ${targetSha}`);
    console.log(`   - Execution Date:   ${executionDate}`);
    console.log(`   - Mutation Score:   ${score !== null ? score + '%' : 'Passed'}`);
    console.log(`   - Break Threshold:  ≥${thresholds.break}%`);
    console.log(`   - Release Decision: PERMITTED`);

    writeStepSummary(`
# 🛡️ Release Mutation Testing Quality Gate Verification

| Metric | Value |
|--------|-------|
| **Target Commit SHA** | \`${targetSha.substring(0, 7)}\` |
| **Analyzed Commit** | \`${targetSha}\` |
| **Execution Date** | ${executionDate} |
| **Mutation Score** | **${score !== null ? score + '%' : 'Passed (≥95%)'}** |
| **Threshold High** | ≥${thresholds.high}% |
| **Threshold Low** | ≥${thresholds.low}% |
| **Threshold Break** | ≥${thresholds.break}% |
| **Status** | ${statusText} |
| **Gate Verdict** | ✅ **RELEASE ALLOWED** |
${runUrl ? `\n[View Full Mutation Testing Run](${runUrl})` : ''}

> **Verification Result**: The target commit satisfies the mutation testing quality gate policy (score ≥ ${thresholds.break}%).
`);
    process.exit(0);
  } else if (detailsFound && !passed) {
    console.error(`❌ RELEASE BLOCKED: Mutation Testing Quality Gate Failed.`);
    console.error(`   - Commit SHA:       ${targetSha}`);
    console.error(`   - Execution Date:   ${executionDate}`);
    console.error(`   - Mutation Score:   ${score !== null ? score + '%' : '<95%'}`);
    console.error(`   - Break Threshold:  ≥${thresholds.break}%`);
    console.error(`   - Release Decision: BLOCKED (Score below break threshold)`);

    writeStepSummary(`
# 🛡️ Release Mutation Testing Quality Gate Verification

| Metric | Value |
|--------|-------|
| **Target Commit SHA** | \`${targetSha.substring(0, 7)}\` |
| **Analyzed Commit** | \`${targetSha}\` |
| **Execution Date** | ${executionDate} |
| **Mutation Score** | **${score !== null ? score + '%' : 'Failed (<95%)'}** |
| **Threshold Break** | ≥${thresholds.break}% |
| **Status** | ${statusText} |
| **Gate Verdict** | ❌ **RELEASE BLOCKED** |
${runUrl ? `\n[View Failed Mutation Testing Run](${runUrl})` : ''}

> [!CAUTION]
> **Release Blocked**: Mutation score is below the mandatory quality gate threshold of **${thresholds.break}%**.
`);
    process.exit(1);
  } else {
    console.error(`❌ RELEASE BLOCKED: No mutation testing result found for commit ${targetSha}.`);
    console.error(`   Ensure the 'Mutation Testing' workflow has run and succeeded on this commit.`);

    writeStepSummary(`
# 🛡️ Release Mutation Testing Quality Gate Verification

| Metric | Value |
|--------|-------|
| **Target Commit SHA** | \`${targetSha.substring(0, 7)}\` |
| **Status** | ❓ **NO RESULT FOUND / PENDING** |
| **Break Threshold** | ≥${thresholds.break}% |
| **Gate Verdict** | ❌ **RELEASE BLOCKED** |

> [!CAUTION]
> **Release Blocked**: No verified mutation testing quality gate result was found associated with commit \`${targetSha}\`.
>
> **Action Required**:
> 1. Run the **Mutation Testing** workflow against this commit on \`main\`.
> 2. Once the workflow completes with a score ≥ ${thresholds.break}%, retry publishing.
`);
    process.exit(1);
  }
}

main().catch(err => {
  console.error(`Unhandled error in release gate validator: ${err.message}`);
  process.exit(1);
});
