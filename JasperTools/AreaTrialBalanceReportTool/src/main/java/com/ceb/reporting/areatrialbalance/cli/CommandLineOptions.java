package com.ceb.reporting.areatrialbalance.cli;

public record CommandLineOptions(
        String inputJsonPath,
        String outputPdfPath,
        String templatePath,
        String compId,
        int reportYear,
        int reportMonth) {
}
