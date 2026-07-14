package com.ceb.reporting.phvobsoleteidle.cli;

public class CommandLineOptions {
    private final String inputJsonPath;
    private final String outputPdfPath;
    private final String templatePath;
    private final String costCenterLabel;
    private final String warehouseCode;
    private final int reportYear;
    private final int reportMonth;

    public CommandLineOptions(String inputJsonPath, String outputPdfPath, String templatePath,
                              String costCenterLabel, String warehouseCode, int reportYear, int reportMonth) {
        this.inputJsonPath = inputJsonPath;
        this.outputPdfPath = outputPdfPath;
        this.templatePath = templatePath;
        this.costCenterLabel = costCenterLabel;
        this.warehouseCode = warehouseCode;
        this.reportYear = reportYear;
        this.reportMonth = reportMonth;
    }

    public String inputJsonPath() { return inputJsonPath; }
    public String outputPdfPath() { return outputPdfPath; }
    public String templatePath() { return templatePath; }
    public String costCenterLabel() { return costCenterLabel; }
    public String warehouseCode() { return warehouseCode; }
    public int reportYear() { return reportYear; }
    public int reportMonth() { return reportMonth; }
}