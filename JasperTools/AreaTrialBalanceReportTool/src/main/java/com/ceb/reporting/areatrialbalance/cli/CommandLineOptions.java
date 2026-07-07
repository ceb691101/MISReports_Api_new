package com.ceb.reporting.areatrialbalance.cli;

public class CommandLineOptions {
    private final String inputJsonPath;
    private final String outputPdfPath;
    private final String templatePath;
    private final String compId;
    private final int reportYear;
    private final int reportMonth;

    public CommandLineOptions(String inputJsonPath, String outputPdfPath, String templatePath,
                              String compId, int reportYear, int reportMonth) {
        this.inputJsonPath = inputJsonPath;
        this.outputPdfPath = outputPdfPath;
        this.templatePath = templatePath;
        this.compId = compId;
        this.reportYear = reportYear;
        this.reportMonth = reportMonth;
    }

    public String inputJsonPath() { return inputJsonPath; }
    public String outputPdfPath() { return outputPdfPath; }
    public String templatePath() { return templatePath; }
    public String compId() { return compId; }
    public int reportYear() { return reportYear; }
    public int reportMonth() { return reportMonth; }
}
