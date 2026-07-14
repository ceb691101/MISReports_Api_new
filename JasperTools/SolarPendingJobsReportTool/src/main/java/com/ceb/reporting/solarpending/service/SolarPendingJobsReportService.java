package com.ceb.reporting.solarpending.service;

import com.ceb.reporting.solarpending.cli.CommandLineOptions;
import com.ceb.reporting.solarpending.model.SolarPendingJobsRow;
import com.ceb.reporting.solarpending.util.ReadPath;
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
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.OffsetDateTime;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class SolarPendingJobsReportService {
    private final ObjectMapper objectMapper = new ObjectMapper();

    public void generate(CommandLineOptions options) throws Exception {
        Path inputPath = Paths.get(options.inputJsonPath());
        Path outputPath = Paths.get(options.outputPdfPath());
        Path templatePath = resolveTemplatePath(options.templatePath());

        List<SolarPendingJobsRow> rows = readRows(inputPath);
        if (rows.isEmpty()) {
            throw new IllegalArgumentException("The input JSON file does not contain any data rows.");
        }

        // Format dates to dd/MM/yyyy
        for (SolarPendingJobsRow row : rows) {
            row.setSubmitDate(formatDate(row.getSubmitDate()));
            row.setPivDate(formatDate(row.getPivDate()));
            row.setPaidDate(formatDate(row.getPaidDate()));
            row.setPiv2PaidDate(formatDate(row.getPiv2PaidDate()));
        }

        System.out.println("Loaded " + rows.size() + " data row(s).");

        // Compile .jrxml template
        System.out.println("Compiling template: " + templatePath);
        JasperReport report = JasperCompileManager.compileReport(templatePath.toString());

        // Set parameters
        Map<String, Object> parameters = new HashMap<>();
        parameters.put("@compId", options.compId());
        parameters.put("@fromDate", options.fromDate());
        parameters.put("@toDate", options.toDate());

        // Fill report
        System.out.println("Filling report...");
        JasperPrint jasperPrint = JasperFillManager.fillReport(
                report,
                parameters,
                new JRBeanCollectionDataSource(rows, false));

        // Export to PDF
        Files.createDirectories(outputPath.getParent());
        exportToPdf(jasperPrint, outputPath);

        if (!looksLikePdf(outputPath)) {
            throw new IllegalStateException("Generated file does not appear to be a valid PDF: " + outputPath);
        }

        System.out.println("Report generated successfully: " + outputPath.toAbsolutePath());
    }

    private List<SolarPendingJobsRow> readRows(Path inputPath) throws IOException {
        return objectMapper.readValue(
                new String(Files.readAllBytes(inputPath), StandardCharsets.UTF_8),
                new TypeReference<List<SolarPendingJobsRow>>() {
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
            config.setMetadataTitle("Solar Retail Rooftop Pending Jobs after PIV2 Paid");
            exporter.setConfiguration(config);

            exporter.exportReport();
        }
    }

    private String formatDate(String value) {
        if (value == null || value.trim().isEmpty()) {
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
        if (configuredPath != null && !configuredPath.trim().isEmpty()) {
            Path configured = Paths.get(configuredPath);
            if (Files.exists(configured)) {
                return configured;
            }
        }

        return Paths.get(fallbackTemplatePath);
    }
}
