package lk.ceb.mis.reporting;
import net.sf.jasperreports.engine.JasperReport;
import net.sf.jasperreports.engine.util.JRLoader;
import net.sf.jasperreports.engine.xml.JRXmlWriter;
import java.io.File;

public class Decompiler {
    public static void main(String[] args) {
        try {
            String source = args[0];
            String dest = args[1];
            JasperReport report = (JasperReport) JRLoader.loadObject(new File(source));
            JRXmlWriter.writeReport(report, dest, "UTF-8");
            System.out.println("Decompiled successfully to " + dest);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
