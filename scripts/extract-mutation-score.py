# Copyright © Erickson Lopez. MIT License.
import json
import os
import glob
import sys
import urllib.request
import urllib.error
from datetime import datetime, timezone

if hasattr(sys.stdout, 'reconfigure'):
    try:
        sys.stdout.reconfigure(encoding='utf-8', errors='backslashreplace')
    except Exception:
        pass

def load_thresholds(config_path="stryker-config.json"):
    thresholds = {"high": 100, "low": 98, "break": 95}
    try:
        if os.path.exists(config_path):
            with open(config_path, "r", encoding="utf-8") as f:
                data = json.load(f)
            t = data.get("stryker-config", {}).get("thresholds", data.get("thresholds", {}))
            thresholds = {
                "high": t.get("high", 100),
                "low": t.get("low", 98),
                "break": t.get("break", 95)
            }
    except Exception as e:
        print(f"Warning: Could not load thresholds from {config_path}: {e}")
    return thresholds

def find_json_reports(target_dir):
    json_files = []
    if not os.path.exists(target_dir):
        return json_files
    for root, _, files in os.walk(target_dir):
        for file in files:
            if (file.endswith(".json") and not file.endswith(".html.json") 
                    and not file.endswith("metadata.json") and not file.startswith("summary-") 
                    and file != "mutation-summary.json"):
                json_files.append(os.path.join(root, file))
    return json_files

def post_commit_status(sha, state, description, context, target_url=""):
    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    repo = os.environ.get("GITHUB_REPOSITORY")
    api_url = os.environ.get("GITHUB_API_URL", "https://api.github.com")

    if not token or not repo or not sha or sha == "unknown":
        print(f"[CommitStatus] Skipping commit status publication (Token: {bool(token)}, Repo: {repo}, SHA: {sha})")
        return

    url = f"{api_url}/repos/{repo}/statuses/{sha}"
    payload = json.dumps({
        "state": state,
        "target_url": target_url or "",
        "description": description[:140],
        "context": context
    }).encode("utf-8")

    req = urllib.request.Request(
        url,
        data=payload,
        headers={
            "User-Agent": "EricksonLopez-Stryker-Gate",
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github+json",
            "Content-Type": "application/json"
        },
        method="POST"
    )

    try:
        with urllib.request.urlopen(req) as resp:
            if 200 <= resp.status < 300:
                print(f"[CommitStatus] Successfully posted status for '{context}' ({state})")
            else:
                print(f"[CommitStatus] Warning: GitHub API responded with {resp.status}")
    except urllib.error.HTTPError as e:
        print(f"[CommitStatus] HTTPError {e.code}: {e.read().decode('utf-8')}")
    except Exception as e:
        print(f"[CommitStatus] Failed to post status: {e}")

def aggregate_results(output_dir="StrykerOutput"):
    thresholds = load_thresholds("stryker-config.json")
    sha = os.environ.get("GITHUB_SHA", "unknown")
    repo = os.environ.get("GITHUB_REPOSITORY", "")
    run_id = os.environ.get("GITHUB_RUN_ID", "")
    server_url = os.environ.get("GITHUB_SERVER_URL", "https://github.com")
    run_url = f"{server_url}/{repo}/actions/runs/{run_id}" if repo and run_id else ""

    summaries = []
    if os.path.exists(output_dir):
        for f in os.listdir(output_dir):
            if f.startswith("summary-") and f.endswith(".json"):
                try:
                    with open(os.path.join(output_dir, f), "r", encoding="utf-8") as fp:
                        summaries.append(json.load(fp))
                except Exception as e:
                    print(f"Warning: Could not parse {f}: {e}")

    total_killed = sum(s.get("mutants_killed", 0) for s in summaries)
    total_mutants = sum(s.get("total_mutants", 0) for s in summaries)
    all_passed = len(summaries) > 0 and all(s.get("passed_break", False) for s in summaries)

    if total_mutants > 0:
        overall_score = round((total_killed / total_mutants) * 10000) / 100
    elif len(summaries) > 0:
        overall_score = 100.0
    else:
        overall_score = 0.0
        all_passed = False

    passed_break = all_passed and overall_score >= thresholds["break"]

    if overall_score >= thresholds["high"]:
        status_label = "✅ HIGH"
    elif overall_score >= thresholds["low"]:
        status_label = "🟡 LOW"
    elif overall_score >= thresholds["break"]:
        status_label = "🟠 WARNING"
    else:
        status_label = "❌ FAILED"

    execution_date = datetime.now(timezone.utc).isoformat()
    aggregated_metadata = {
        "commit_sha": sha,
        "execution_date": execution_date,
        "overall_mutation_score": overall_score,
        "total_mutants_killed": total_killed,
        "total_mutants": total_mutants,
        "threshold_high": thresholds["high"],
        "threshold_low": thresholds["low"],
        "threshold_break": thresholds["break"],
        "status": status_label,
        "passed_break": passed_break,
        "run_url": run_url,
        "packages": summaries
    }

    os.makedirs(output_dir, exist_ok=True)
    with open(os.path.join(output_dir, "mutation-summary.json"), "w", encoding="utf-8") as fp:
        json.dump(aggregated_metadata, fp, indent=2)

    step_summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if step_summary_path:
        table_rows = "\n".join(
            f"| **{s.get('package', 'unknown')}** | **{s.get('mutation_score', 0)}%** | {s.get('mutants_killed', 0)}/{s.get('total_mutants', 0)} | {s.get('status', 'unknown')} |"
            for s in summaries
        )
        summary_md = f"""
# 🛡️ Consolidated Mutation Testing Quality Gate Summary

| Metric | Value |
|--------|-------|
| **Overall Mutation Score** | **{overall_score}%** |
| **Total Mutants Killed** | {total_killed} |
| **Total Mutants** | {total_mutants} |
| **Threshold High** | ≥{thresholds['high']}% |
| **Threshold Low** | ≥{thresholds['low']}% |
| **Threshold Break** | ≥{thresholds['break']}% |
| **Quality Gate Status** | {status_label} |
| **Commit SHA** | `{sha[:7]}` |
| **Execution Date** | {execution_date} |

### Package Breakdown

| Package | Mutation Score | Mutants Killed / Total | Status |
|---------|----------------|------------------------|--------|
{table_rows or '| *No package summaries found* | - | - | - |'}

> **Quality Gate Policy**: Mutation score must be ≥ {thresholds['break']}% (Break threshold) for release eligibility.
"""
        with open(step_summary_path, "a", encoding="utf-8") as fp:
            fp.write(summary_md)

    output_path = os.environ.get("GITHUB_OUTPUT")
    if output_path:
        with open(output_path, "a", encoding="utf-8") as fp:
            fp.write(f"overall_score={overall_score}\n")
            fp.write(f"passed_gate={'true' if passed_break else 'false'}\n")
            fp.write(f"status={status_label}\n")
            fp.write(f"total_killed={total_killed}\n")
            fp.write(f"total_mutants={total_mutants}\n")

    commit_status_state = "success" if passed_break else "failure"
    desc = f"Mutation Score: {overall_score}% ({total_killed}/{total_mutants}) - {status_label}"
    post_commit_status(sha, commit_status_state, desc, "quality-gate/mutation-testing", run_url)

    print(f"[Consolidated Quality Gate] Overall Score: {overall_score}% ({total_killed}/{total_mutants}) - {status_label}")
    if not passed_break:
        print(f"[Consolidated Quality Gate] FAILED: Overall mutation score {overall_score}% is below break threshold {thresholds['break']}% or a package failed.")
        sys.exit(1)

def record_single_package(target_dir, pkg_name, config_file):
    thresholds = load_thresholds(config_file)
    score = 0.0
    killed = 0
    total = 0
    found_report = False

    json_files = find_json_reports(target_dir)
    if json_files:
        try:
            with open(json_files[0], "r", encoding="utf-8") as f:
                data = json.load(f)
            if "mutationScore" in data:
                score = float(data["mutationScore"])
            files = data.get("files", {})
            for f_val in files.values():
                for m in f_val.get("mutants", []):
                    st = str(m.get("status", "")).lower()
                    if st in ["killed", "timeout"]:
                        killed += 1
                        total += 1
                    elif st in ["survived", "nocoverage"]:
                        total += 1
            if total > 0 and "mutationScore" not in data:
                score = round((killed / total) * 10000) / 100
            elif total == 0:
                score = 100.0
            found_report = True
        except Exception as e:
            print(f"Warning: Error parsing {json_files[0]}: {e}")

    passed_gate = found_report and (score >= thresholds["break"] or total == 0)

    if found_report:
        if score >= thresholds["high"] or total == 0:
            status_label = "✅ HIGH"
        elif score >= thresholds["low"]:
            status_label = "🟡 LOW"
        elif score >= thresholds["break"]:
            status_label = "🟠 WARNING"
        else:
            status_label = "❌ FAILED"
    else:
        status_label = "❌ FAILED"

    sha = os.environ.get("GITHUB_SHA", "unknown")
    repo = os.environ.get("GITHUB_REPOSITORY", "")
    run_id = os.environ.get("GITHUB_RUN_ID", "")
    server_url = os.environ.get("GITHUB_SERVER_URL", "https://github.com")
    run_url = f"{server_url}/{repo}/actions/runs/{run_id}" if repo and run_id else ""
    execution_date = datetime.now(timezone.utc).isoformat()

    metadata = {
        "package": pkg_name,
        "commit_sha": sha,
        "execution_date": execution_date,
        "mutation_score": score,
        "mutants_killed": killed,
        "total_mutants": total,
        "threshold_high": thresholds["high"],
        "threshold_low": thresholds["low"],
        "threshold_break": thresholds["break"],
        "status": status_label,
        "passed_break": passed_gate,
        "run_url": run_url
    }

    os.makedirs("StrykerOutput", exist_ok=True)
    with open(os.path.join("StrykerOutput", f"summary-{pkg_name}.json"), "w", encoding="utf-8") as fp:
        json.dump(metadata, fp, indent=2)

    step_summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if step_summary_path:
        summary_md = f"""
## 🛡️ Stryker Mutation Testing Results — {pkg_name}

| Metric | Value |
|--------|-------|
| **Mutation Score** | **{score}%** |
| **Mutants Killed** | {killed} |
| **Total Mutants** | {total} |
| **Threshold High** | ≥{thresholds['high']}% |
| **Threshold Low** | ≥{thresholds['low']}% |
| **Threshold Break** | ≥{thresholds['break']}% |
| **Status** | {status_label} |
| **Commit SHA** | `{sha[:7]}` |
| **Execution date** | {execution_date} |
"""
        with open(step_summary_path, "a", encoding="utf-8") as fp:
            fp.write(summary_md)

    output_path = os.environ.get("GITHUB_OUTPUT")
    if output_path:
        with open(output_path, "a", encoding="utf-8") as fp:
            fp.write(f"score={score}\n")
            fp.write(f"passed_gate={'true' if passed_gate else 'false'}\n")
            fp.write(f"status={status_label}\n")
            fp.write(f"killed={killed}\n")
            fp.write(f"total={total}\n")

    commit_status_state = "success" if passed_gate else "failure"
    desc = f"[{pkg_name}] Score: {score}% ({killed}/{total}) - {status_label}"
    post_commit_status(sha, commit_status_state, desc, f"quality-gate/mutation-testing/{pkg_name}", run_url)

    print(f"[{pkg_name}] Stryker Score: {score}% ({killed}/{total}) - {status_label}")
    if not passed_gate:
        print(f"[{pkg_name}] FAILED: Mutation score {score}% is below break threshold {thresholds['break']}%.")
        sys.exit(1)

def main():
    if len(sys.argv) > 1 and sys.argv[1] in ["--aggregate", "-a"]:
        output_dir = sys.argv[2] if len(sys.argv) > 2 else "StrykerOutput"
        aggregate_results(output_dir)
    else:
        target_dir = sys.argv[1] if len(sys.argv) > 1 else "StrykerOutput/ci"
        pkg_name = sys.argv[2] if len(sys.argv) > 2 else "Core"
        config_file = sys.argv[3] if len(sys.argv) > 3 else "stryker-config.json"
        record_single_package(target_dir, pkg_name, config_file)

if __name__ == "__main__":
    main()
