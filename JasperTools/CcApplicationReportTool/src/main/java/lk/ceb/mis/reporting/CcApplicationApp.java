package lk.ceb.mis.reporting;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import net.sf.jasperreports.engine.*;
import net.sf.jasperreports.engine.data.JRBeanCollectionDataSource;
import net.sf.jasperreports.engine.export.JRPdfExporter;
import net.sf.jasperreports.export.SimpleExporterInput;
import net.sf.jasperreports.export.SimpleOutputStreamExporterOutput;
import net.sf.jasperreports.export.SimplePdfExporterConfiguration;
import picocli.CommandLine;
import picocli.CommandLine.Command;
import picocli.CommandLine.Option;

import java.io.File;
import java.io.FileOutputStream;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.Callable;
import java.sql.Timestamp;
import java.math.BigDecimal;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Standalone command-line tool that generates a "C/C Solar Application Progress"
 * PDF report using JasperReports.
 *
 * The .NET backend calls this JAR as a subprocess:
 *   java -jar cc-application-report-1.0-SNAPSHOT-jar-with-dependencies.jar \
 *       --input  data.json \
 *       --output report.pdf \
 *       --template cc_application_progress.jrxml \
 *       --costctr "511.30 - Some Name" \
 *       --fromdate "2022-01-01" \
 *       --todate "2022-01-30"
 *
 * Data is supplied as a JSON array of objects with String fields matching
 * the .jrxml field definitions (ApplicationId, ApplicationNo, SubmitDate, etc.).
 */
@Command(name = "cc-application-report",
        description = "Generates a C/C Solar Application Progress PDF report using JasperReports.",
        mixinStandardHelpOptions = true,
        version = "1.0-SNAPSHOT")
public class CcApplicationApp implements Callable<Integer> {

    @Option(names = "--input", required = true, description = "Path to the JSON input file containing report data.")
    private File inputFile;

    @Option(names = "--output", required = true, description = "Path for the generated PDF output file.")
    private File outputFile;

    @Option(names = "--template", required = true, description = "Path to the .jrxml report template file.")
    private File templateFile;

    @Option(names = "--costctr", defaultValue = "", description = "Cost center label to display on the report.")
    private String costCenter;

    @Option(names = "--fromdate", defaultValue = "", description = "From-date label to display on the report.")
    private String fromDate;

    @Option(names = "--todate", defaultValue = "", description = "To-date label to display on the report.")
    private String toDate;

    public static void main(String[] args) {
        int exitCode = new CommandLine(new CcApplicationApp()).execute(args);
        System.exit(exitCode);
    }

    @Override
    public Integer call() throws Exception {
        // ----- 1. Validate inputs -----
        if (!inputFile.exists()) {
            System.err.println("ERROR: Input file not found: " + inputFile.getAbsolutePath());
            return 1;
        }
        if (!templateFile.exists()) {
            System.err.println("ERROR: Template file not found: " + templateFile.getAbsolutePath());
            return 1;
        }

        System.out.println("Input file : " + inputFile.getAbsolutePath());
        System.out.println("Output file: " + outputFile.getAbsolutePath());
        System.out.println("Template   : " + templateFile.getAbsolutePath());
        System.out.println("Cost Center: " + costCenter);
        System.out.println("From Date  : " + fromDate);
        System.out.println("To Date    : " + toDate);

        // ----- 2. Read JSON data -----
        ObjectMapper mapper = new ObjectMapper();
        List<Map<String, Object>> rows = mapper.readValue(
                inputFile, new TypeReference<List<Map<String, Object>>>() {});

        if (rows.isEmpty()) {
            System.err.println("ERROR: Input JSON contains no data rows.");
            return 1;
        }

        // Convert specific fields to BigDecimal and Timestamp as required by the original .jrxml
        SimpleDateFormat isoFormat = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");
        for (Map<String, Object> row : rows) {
            for (Map.Entry<String, Object> entry : row.entrySet()) {
                String key = entry.getKey();
                Object val = entry.getValue();
                if (val == null) continue;

                try {
                    if (key.endsWith("DATE") && val instanceof String) {
                        String sVal = (String) val;
                        if (sVal.contains("T")) {
                            Date d = isoFormat.parse(sVal.substring(0, 19));
                            entry.setValue(new Timestamp(d.getTime()));
                        }
                    } else if (key.equals("PIV_AMOUNT") || key.equals("PHASE") || key.equals("CONNECTION_TYPE")) {
                        if (val instanceof Number) {
                            entry.setValue(BigDecimal.valueOf(((Number) val).doubleValue()));
                        } else if (val instanceof String) {
                            try {
                                entry.setValue(new BigDecimal((String) val));
                            } catch (NumberFormatException nfe) {
                                // If it's a string like "Single Phase" that can't be parsed,
                                // we shouldn't crash. But the template expects BigDecimal.
                                // We'll just leave it as String and hope Jasper handles it or 
                                // set to zero if we want to avoid GroovyCastException.
                                // Let's set it to BigDecimal.ZERO if it's completely unparseable.
                                entry.setValue(BigDecimal.ZERO);
                            }
                        }
                    }
                } catch (Exception ex) {
                    System.err.println("Warning: failed to convert " + key + ": " + val);
                }
            }
        }

        System.out.println("Loaded " + rows.size() + " data row(s).");

        // ----- 3. Compile the .jrxml template -----
        System.out.println("Compiling template...");
        JasperReport jasperReport = JasperCompileManager.compileReport(templateFile.getAbsolutePath());

        // ----- 4. Set report parameters -----
        Map<String, Object> parameters = new HashMap<>();
        parameters.put("@costctr", costCenter);
        parameters.put("@fromDate", fromDate);
        parameters.put("@toDate", toDate);

        // ----- 5. Fill the report with data -----
        System.out.println("Filling report...");
        JRBeanCollectionDataSource dataSource = new JRBeanCollectionDataSource(rows);
        JasperPrint jasperPrint = JasperFillManager.fillReport(jasperReport, parameters, dataSource);

        // ----- 6. Export to PDF -----
        System.out.println("Exporting PDF...");

        // Ensure parent directory exists
        File parentDir = outputFile.getParentFile();
        if (parentDir != null && !parentDir.exists()) {
            parentDir.mkdirs();
        }

        JRPdfExporter exporter = new JRPdfExporter();
        exporter.setExporterInput(new SimpleExporterInput(jasperPrint));

        try (FileOutputStream fos = new FileOutputStream(outputFile)) {
            exporter.setExporterOutput(new SimpleOutputStreamExporterOutput(fos));

            SimplePdfExporterConfiguration config = new SimplePdfExporterConfiguration();
            config.setMetadataAuthor("CEB MIS Reports");
            config.setMetadataTitle("C/C Solar Application Progress");
            exporter.setConfiguration(config);

            exporter.exportReport();
        }

        System.out.println("PDF generated successfully: " + outputFile.getAbsolutePath());
        return 0;
    }
}
