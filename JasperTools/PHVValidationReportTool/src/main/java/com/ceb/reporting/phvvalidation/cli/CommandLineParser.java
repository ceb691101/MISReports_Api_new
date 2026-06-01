package com.ceb.reporting.phvvalidation.cli;

import java.util.HashMap;
import java.util.Map;

public final class CommandLineParser {
    private CommandLineParser() {
    }

    public static CommandLineOptions parse(String[] args) {
        Map<String, String> values = new HashMap<>();

        for (int index = 0; index < args.length; index++) {
            String token = args[index];
            if (!token.startsWith("--")) {
                continue;
            }

            String key = token.substring(2).toLowerCase();
            String value = "true";

            if (index + 1 < args.length && !args[index + 1].startsWith("--")) {
                value = args[++index];
            }

            values.put(key, value);
        }

        String inputJsonPath = required(values, "input");
        String outputPdfPath = required(values, "output");
        String templatePath = required(values, "template");
        String costCenterLabel = required(values, "costctr");
        int reportYear = parseInt(required(values, "repyear"), "repyear");
        int reportMonth = parseInt(required(values, "repmonth"), "repmonth");

        return new CommandLineOptions(
                inputJsonPath,
                outputPdfPath,
                templatePath,
                costCenterLabel,
                reportYear,
                reportMonth);
    }

    private static String required(Map<String, String> values, String key) {
        String value = values.get(key);
        if (value == null || value.isBlank()) {
            throw new IllegalArgumentException("Missing required argument --" + key);
        }

        return value;
    }

    private static int parseInt(String value, String key) {
        try {
            return Integer.parseInt(value);
        } catch (NumberFormatException ex) {
            throw new IllegalArgumentException("Argument --" + key + " must be a valid integer.", ex);
        }
    }
}