package com.ceb.reporting.ccapplication.service;

import com.ceb.reporting.ccapplication.cli.CommandLineOptions;
import com.ceb.reporting.ccapplication.model.CcApplicationRow;
import com.ceb.reporting.ccapplication.util.ReadPath;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import net.sf.jasperreports.engine.data.JRBeanCollectionDataSource;
import net.sf.jasperreports.engine.JasperCompileManager;
import net.sf.jasperreports.engine.JasperFillManager;
import net.sf.jasperreports.engine.JasperPrint;
import net.sf.jasperreports.engine.JasperReport;
import net.sf.jasperreports.engine.export.JRPdfExporter;
import net.sf.jasperreports.export.SimpleExporterInput;
import net.sf.jasperreports.export.SimpleOutputStreamExporterOutput;
import net.sf.jasperreports.export.SimplePdfExporterConfiguration;

import java.io.FileOutputStream;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.OffsetDateTime;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class CcApplicationReportService {
    private final ObjectMapper objectMapper = new ObjectMapper();

    public void generate(CommandLineOptions options) throws Exception {
        Path inputPath = Path.of(options.inputJsonPath());
        Path outputPath = Path.of(options.outputPdfPath());
        Path templatePath = resolveTemplatePath(options.templatePath());

        List<CcApplicationRow> rows = readRows(inputPath);
        if (rows.isEmpty()) {
            throw new IllegalArgumentException("The input JSON file does not contain any data rows.");
        }

        // Format date fields from ISO to dd/MM/yyyy for display
        for (CcApplicationRow row : rows) {
            row.setPiv1Date(formatDate(row.getPiv1Date()));
            row.setPaidDate(formatDate(row.getPaidDate()));
        }

        System.out.println("Loaded " + rows.size() + " data row(s).");

        // Compile the .jrxml template
        System.out.println("Compiling template: " + templatePath);
        JasperReport report = JasperCompileManager.compileReport(templatePath.toString());

        // Set report parameters
        Map<String, Object> parameters = new HashMap<>();
        parameters.put("@costctr", options.costCenterLabel());
        parameters.put("@fromDate", options.fromDate());
        parameters.put("@toDate", options.toDate());

        // Fill the report with data
        System.out.println("Filling report...");
        JasperPrint jasperPrint = JasperFillManager.fillReport(
                report,
                parameters,
                new JRBeanCollectionDataSource(rows, false));

        // Export
        Files.createDirectories(outputPath.getParent());
        String outStr = outputPath.toString().toLowerCase();
        if (outStr.endsWith(".csv")) {
            net.sf.jasperreports.engine.export.JRCsvExporter exporter =
                    new net.sf.jasperreports.engine.export.JRCsvExporter();
            exporter.setExporterInput(
                    new net.sf.jasperreports.export.SimpleExporterInput(jasperPrint));
            exporter.setExporterOutput(
                    new net.sf.jasperreports.export.SimpleWriterExporterOutput(outputPath.toFile()));
            exporter.exportReport();
        } else {
            exportToPdf(jasperPrint, outputPath);
            if (!looksLikePdf(outputPath)) {
                throw new IllegalStateException(
                        "Generated file does not appear to be a valid PDF: " + outputPath);
            }
        }

        System.out.println("Report generated successfully: " + outputPath.toAbsolutePath());
    }

    private List<CcApplicationRow> readRows(Path inputPath) throws IOException {
        return objectMapper.readValue(
                Files.readString(inputPath),
                new TypeReference<List<CcApplicationRow>>() {
                });
    }

    private void exportToPdf(JasperPrint jasperPrint, Path outputPath) throws Exception {
        System.out.println("Exporting PDF...");
        JRPdfExporter exporter = new JRPdfExporter();
        exporter.setExporterInput(new SimpleExporterInput(jasperPrint));

        try (FileOutputStream fos = new FileOutputStream(outputPath.toFile())) {
            exporter.setExporterOutput(new SimpleOutputStreamExporterOutput(fos));

            SimplePdfExporterConfiguration config = new SimplePdfExporterConfiguration();
            config.setMetadataAuthor("CEB MIS Reports");
            config.setMetadataTitle("C/C Solar Application Progress");
            exporter.setConfiguration(config);

            exporter.exportReport();
        }
    }

    private String formatDate(String value) {
        if (value == null || value.isBlank()) {
            return value;
        }

        try {
            return OffsetDateTime.parse(value)
                    .format(DateTimeFormatter.ofPattern("dd/MM/yyyy"));
        } catch (Exception ignored) {
        }

        try {
            return LocalDateTime.parse(value)
                    .format(DateTimeFormatter.ofPattern("dd/MM/yyyy"));
        } catch (Exception ignored) {
        }

        return value;
    }

    private boolean looksLikePdf(Path outputPath) throws IOException {
        byte[] firstBytes = Files.readAllBytes(outputPath);
        return firstBytes.length >= 5
                && firstBytes[0] == '%'
                && firstBytes[1] == 'P'
                && firstBytes[2] == 'D'
                && firstBytes[3] == 'F'
                && firstBytes[4] == '-';
    }

    private Path resolveTemplatePath(String fallbackTemplatePath) throws IOException {
        String configuredPath = new ReadPath().getPath();
        if (configuredPath != null && !configuredPath.isBlank()) {
            Path configured = Path.of(configuredPath);
            if (Files.exists(configured)) {
                return configured;
            }
        }

        return Path.of(fallbackTemplatePath);
    }
}
