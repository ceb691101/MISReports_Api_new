package com.ceb.reporting.solarpending.cli;

public class CommandLineOptions {
    private final String inputJsonPath;
    private final String outputPdfPath;
    private final String templatePath;
    private final String compId;
    private final String fromDate;
    private final String toDate;

    public CommandLineOptions(String inputJsonPath, String outputPdfPath, String templatePath,
                              String compId, String fromDate, String toDate) {
        this.inputJsonPath = inputJsonPath;
        this.outputPdfPath = outputPdfPath;
        this.templatePath = templatePath;
        this.compId = compId;
        this.fromDate = fromDate;
        this.toDate = toDate;
    }

    public String inputJsonPath() { return inputJsonPath; }
    public String outputPdfPath() { return outputPdfPath; }
    public String templatePath() { return templatePath; }
    public String compId() { return compId; }
    public String fromDate() { return fromDate; }
    public String toDate() { return toDate; }
}
