package com.ceb.reporting.ccapplication.cli;

public class CommandLineOptions {
    private final String inputJsonPath;
    private final String outputPdfPath;
    private final String templatePath;
    private final String costCenterLabel;
    private final String fromDate;
    private final String toDate;

    public CommandLineOptions(String inputJsonPath, String outputPdfPath, String templatePath,
                              String costCenterLabel, String fromDate, String toDate) {
        this.inputJsonPath = inputJsonPath;
        this.outputPdfPath = outputPdfPath;
        this.templatePath = templatePath;
        this.costCenterLabel = costCenterLabel;
        this.fromDate = fromDate;
        this.toDate = toDate;
    }

    public String inputJsonPath() { return inputJsonPath; }
    public String outputPdfPath() { return outputPdfPath; }
    public String templatePath() { return templatePath; }
    public String costCenterLabel() { return costCenterLabel; }
    public String fromDate() { return fromDate; }
    public String toDate() { return toDate; }
}
