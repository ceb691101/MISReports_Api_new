package com.ceb.reporting.phvobsoleteidle;

import com.ceb.reporting.phvobsoleteidle.cli.CommandLineOptions;
import com.ceb.reporting.phvobsoleteidle.cli.CommandLineParser;
import com.ceb.reporting.phvobsoleteidle.service.PhvObsoleteIdleReportService;

public class Main {
    public static void main(String[] args) {
        try {
            CommandLineOptions options = CommandLineParser.parse(args);
            new PhvObsoleteIdleReportService().generate(options);
        } catch (Exception ex) {
            System.err.println(ex.getMessage());
            ex.printStackTrace(System.err);
            System.exit(1);
        }
    }
}