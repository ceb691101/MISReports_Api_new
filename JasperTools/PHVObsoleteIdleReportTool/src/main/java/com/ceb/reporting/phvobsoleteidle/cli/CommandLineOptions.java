package com.ceb.reporting.phvobsoleteidle.cli;

public record CommandLineOptions(
        String inputJsonPath,
        String outputPdfPath,
        String templatePath,
        String costCenterLabel,
        String warehouseCode,
        int reportYear,
        int reportMonth) {
}