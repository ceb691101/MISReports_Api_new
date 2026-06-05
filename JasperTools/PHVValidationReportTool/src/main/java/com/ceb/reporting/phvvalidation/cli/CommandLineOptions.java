package com.ceb.reporting.phvvalidation.cli;

public record CommandLineOptions(
        String inputJsonPath,
        String outputPdfPath,
        String templatePath,
        String costCenterLabel,
        int reportYear,
        int reportMonth) {
}