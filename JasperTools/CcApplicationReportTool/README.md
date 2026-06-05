# CC Application Report Tool

Standalone Java/JasperReports CLI used by the .NET Web API to render the C/C Solar Application Progress PDF report from JSON input.

## Build

```bash
mvn -DskipTests package
```

## Output

```text
target/cc-application-report.jar
```

## Usage

```bash
java -jar target/cc-application-report.jar \
    --input  data.json \
    --output report.pdf \
    --template cc_application_progress.jrxml \
    --costctr "511.30" \
    --fromdate "2022-01-01" \
    --todate "2022-01-30"
```

## Arguments

| Argument     | Required | Description                              |
|-------------|----------|------------------------------------------|
| `--input`   | Yes      | Path to the JSON input file              |
| `--output`  | Yes      | Path for the generated PDF/CSV output    |
| `--template`| Yes      | Path to the .jrxml report template file  |
| `--costctr` | No       | Cost center label for the report header  |
| `--fromdate`| No       | From-date label for the report header    |
| `--todate`  | No       | To-date label for the report header      |
