package com.ceb.reporting.areatrialbalance.model;

import com.fasterxml.jackson.annotation.JsonAlias;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.math.BigDecimal;

public class AreaTrialBalanceRow {
    @JsonProperty("ac_cd")
    @JsonAlias({"AccountCode", "acCd"})
    private String ac_cd;

    @JsonProperty("gl_nm")
    @JsonAlias({"AccountName", "glNm"})
    private String gl_nm;

    @JsonProperty("titile_flag")
    @JsonAlias({"TitleFlag", "titileFlag"})
    private String titile_flag;

    @JsonProperty("op_sbal")
    @JsonAlias({"OpeningBalance", "opSbal"})
    private BigDecimal op_sbal;

    @JsonProperty("dr_samt")
    @JsonAlias({"DebitAmount", "drSamt"})
    private BigDecimal dr_samt;

    @JsonProperty("cr_samt")
    @JsonAlias({"CreditAmount", "crSamt"})
    private BigDecimal cr_samt;

    @JsonProperty("cl_sbal")
    @JsonAlias({"ClosingBalance", "clSbal"})
    private BigDecimal cl_sbal;

    @JsonProperty("cct_name")
    @JsonAlias({"CompanyName", "cctName"})
    private String cct_name;

    // Getters and Setters for snake_case (matching JRXML fields)
    public String getAc_cd() {
        return ac_cd;
    }

    public void setAc_cd(String ac_cd) {
        this.ac_cd = ac_cd;
    }

    public String getGl_nm() {
        return gl_nm;
    }

    public void setGl_nm(String gl_nm) {
        this.gl_nm = gl_nm;
    }

    public String getTitile_flag() {
        return titile_flag;
    }

    public void setTitile_flag(String titile_flag) {
        this.titile_flag = titile_flag;
    }

    public BigDecimal getOp_sbal() {
        return op_sbal;
    }

    public void setOp_sbal(BigDecimal op_sbal) {
        this.op_sbal = op_sbal;
    }

    public BigDecimal getDr_samt() {
        return dr_samt;
    }

    public void setDr_samt(BigDecimal dr_samt) {
        this.dr_samt = dr_samt;
    }

    public BigDecimal getCr_samt() {
        return cr_samt;
    }

    public void setCr_samt(BigDecimal cr_samt) {
        this.cr_samt = cr_samt;
    }

    public BigDecimal getCl_sbal() {
        return cl_sbal;
    }

    public void setCl_sbal(BigDecimal cl_sbal) {
        this.cl_sbal = cl_sbal;
    }

    public String getCct_name() {
        return cct_name;
    }

    public void setCct_name(String cct_name) {
        this.cct_name = cct_name;
    }

    // Getters for camelCase (for convenience and standard Java bean mapping)
    public String getAcCd() {
        return ac_cd;
    }

    public String getGlNm() {
        return gl_nm;
    }

    public String getTitileFlag() {
        return titile_flag;
    }

    public BigDecimal getOpSbal() {
        return op_sbal;
    }

    public BigDecimal getDrSamt() {
        return dr_samt;
    }

    public BigDecimal getCrSamt() {
        return cr_samt;
    }

    public BigDecimal getClSbal() {
        return cl_sbal;
    }

    public String getCctName() {
        return cct_name;
    }
}
