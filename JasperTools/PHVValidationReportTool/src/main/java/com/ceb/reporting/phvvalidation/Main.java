package com.ceb.reporting.phvvalidation;

import com.ceb.reporting.phvvalidation.cli.CommandLineOptions;
import com.ceb.reporting.phvvalidation.cli.CommandLineParser;
import com.ceb.reporting.phvvalidation.service.PhvValidationReportService;

public final class Main {
    private Main() {
    }

    public static void main(String[] args) {
        try {
            CommandLineOptions options = CommandLineParser.parse(args);
            new PhvValidationReportService().generate(options);
            System.out.println("PHV validation PDF generated successfully.");
        } catch (Exception ex) {
            System.err.println(ex.getMessage());
            ex.printStackTrace(System.err);
            System.exit(1);
        }
    }
}