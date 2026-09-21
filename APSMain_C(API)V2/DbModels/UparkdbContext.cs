using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace APSMain.DbModels;

public partial class UparkdbContext : DbContext
{
    public UparkdbContext()
    {
    }

    public UparkdbContext(DbContextOptions<UparkdbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Taccept> Taccepts { get; set; }

    public virtual DbSet<Taccountinfo> Taccountinfos { get; set; }

    public virtual DbSet<Tapswelfare> Tapswelfares { get; set; }

    public virtual DbSet<Tautoticket> Tautotickets { get; set; }

    public virtual DbSet<Tbcardinfo> Tbcardinfos { get; set; }

    public virtual DbSet<Tblackfee> Tblackfees { get; set; }

    public virtual DbSet<Tblacklist> Tblacklists { get; set; }

    public virtual DbSet<Tcompany> Tcompanies { get; set; }

    public virtual DbSet<Tcuponinfo> Tcuponinfos { get; set; }

    public virtual DbSet<Tdayclosecal> Tdayclosecals { get; set; }

    public virtual DbSet<Tdepartment> Tdepartments { get; set; }

    public virtual DbSet<Tdevicealarm> Tdevicealarms { get; set; }

    public virtual DbSet<Tdevicelist> Tdevicelists { get; set; }

    public virtual DbSet<Tdevicestate> Tdevicestates { get; set; }

    public virtual DbSet<Tdiscount> Tdiscounts { get; set; }

    public virtual DbSet<Tdiscountaccount> Tdiscountaccounts { get; set; }

    public virtual DbSet<Tdiscountdept> Tdiscountdepts { get; set; }

    public virtual DbSet<Tdiscountinfo> Tdiscountinfos { get; set; }

    public virtual DbSet<Tdiscountissue> Tdiscountissues { get; set; }

    public virtual DbSet<Tdiscountreport> Tdiscountreports { get; set; }

    public virtual DbSet<Tdiscountsale> Tdiscountsales { get; set; }

    public virtual DbSet<Tdiscounttable> Tdiscounttables { get; set; }

    public virtual DbSet<Tdisperson> Tdispeople { get; set; }

    public virtual DbSet<Tdisperson2> Tdisperson2s { get; set; }

    public virtual DbSet<Tgateinfo> Tgateinfos { get; set; }

    public virtual DbSet<Tholiday> Tholidays { get; set; }

    public virtual DbSet<Tjsalecode> Tjsalecodes { get; set; }

    public virtual DbSet<Tlogon> Tlogons { get; set; }

    public virtual DbSet<Tlprconfig> Tlprconfigs { get; set; }

    public virtual DbSet<Tmanager> Tmanagers { get; set; }

    public virtual DbSet<Tmisuinfo> Tmisuinfos { get; set; }

    public virtual DbSet<Tmoneyerrlog> Tmoneyerrlogs { get; set; }

    public virtual DbSet<Tnocar> Tnocars { get; set; }

    public virtual DbSet<Tnocarnum> Tnocarnums { get; set; }

    public virtual DbSet<Tocssale> Tocssales { get; set; }

    public virtual DbSet<Topclosecal> Topclosecals { get; set; }

    public virtual DbSet<Tparkconfig> Tparkconfigs { get; set; }

    public virtual DbSet<Tparkemergency> Tparkemergencies { get; set; }

    public virtual DbSet<Tparkfee> Tparkfees { get; set; }

    public virtual DbSet<Tparkin> Tparkins { get; set; }

    public virtual DbSet<Tparkinfo> Tparkinfos { get; set; }

    public virtual DbSet<Tparkingname> Tparkingnames { get; set; }

    public virtual DbSet<Tparknum> Tparknums { get; set; }

    public virtual DbSet<Tparktitle> Tparktitles { get; set; }

    public virtual DbSet<Tparkvaliable> Tparkvaliables { get; set; }

    public virtual DbSet<Tparkvariable> Tparkvariables { get; set; }

    public virtual DbSet<Tperiodaccount> Tperiodaccounts { get; set; }

    public virtual DbSet<Tperiodfee> Tperiodfees { get; set; }

    public virtual DbSet<Tperiodin> Tperiodins { get; set; }

    public virtual DbSet<Tperiodinout> Tperiodinouts { get; set; }

    public virtual DbSet<Tperiodmember> Tperiodmembers { get; set; }

    public virtual DbSet<Tperiodparktime> Tperiodparktimes { get; set; }

    public virtual DbSet<Tperiodtmember> Tperiodtmembers { get; set; }

    public virtual DbSet<Tping> Tpings { get; set; }

    public virtual DbSet<Ttcardinfo> Ttcardinfos { get; set; }

    public virtual DbSet<Tvaninfo> Tvaninfos { get; set; }

    public virtual DbSet<Txblacklist> Txblacklists { get; set; }

    public virtual DbSet<Txpark> Txparks { get; set; }

    public virtual DbSet<Txparknum> Txparknums { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;database=uparkdb;user=uparkdb;password=!@Uparkdb1004", Microsoft.EntityFrameworkCore.ServerVersion.Parse("5.7.44-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8_general_ci")
            .HasCharSet("utf8");

        modelBuilder.Entity<Taccept>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Accepttime })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("taccept");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Accepttime)
                .HasDefaultValueSql("'0000-00-00 00:00:00'")
                .HasColumnType("datetime")
                .HasColumnName("accepttime");
            entity.Property(e => e.Accepttype)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("accepttype");
            entity.Property(e => e.Devicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Devicetype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("devicetype");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Ip)
                .HasMaxLength(20)
                .HasColumnName("ip");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Taccountinfo>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("taccountinfo");

            entity.HasIndex(e => e.Id, "id_idx");

            entity.HasIndex(e => e.Salecode, "salecode_idx");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .HasColumnName("id");
            entity.Property(e => e.Regdate)
                .HasColumnType("datetime")
                .HasColumnName("regdate");
            entity.Property(e => e.Salecode)
                .HasColumnType("int(11)")
                .HasColumnName("salecode");
            entity.Property(e => e.Salecount)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("salecount");
            entity.Property(e => e.Salemaxnum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("salemaxnum");
        });

        modelBuilder.Entity<Tapswelfare>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Apsnum, e.Apsip })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

            entity.ToTable("tapswelfare");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Apsnum)
                .HasColumnType("int(11)")
                .HasColumnName("apsnum");
            entity.Property(e => e.Apsip)
                .HasMaxLength(30)
                .HasColumnName("apsip");
            entity.Property(e => e.Apsname)
                .HasMaxLength(50)
                .HasColumnName("apsname");
            entity.Property(e => e.Groupnum)
                .HasColumnType("int(11)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Parkinfo)
                .HasColumnType("blob")
                .HasColumnName("parkinfo");
            entity.Property(e => e.Sitenum)
                .HasColumnType("int(11)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Status)
                .HasMaxLength(45)
                .HasColumnName("status");
        });

        modelBuilder.Entity<Tautoticket>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tautoticket");

            entity.HasIndex(e => e.Xindex, "xindex_UNIQUE").IsUnique();

            entity.Property(e => e.Xindex)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Asalenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("asalenum");
            entity.Property(e => e.Aticketnum)
                .HasDefaultValueSql("'90001'")
                .HasColumnType("int(11)")
                .HasColumnName("aticketnum");
        });

        modelBuilder.Entity<Tbcardinfo>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Dealdate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tbcardinfo");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Dealdate).HasColumnName("dealdate");
            entity.Property(e => e.Acceptnum)
                .HasMaxLength(14)
                .HasColumnName("acceptnum");
            entity.Property(e => e.Accepttype)
                .HasColumnType("smallint(6)")
                .HasColumnName("accepttype");
            entity.Property(e => e.Branchnum)
                .HasMaxLength(20)
                .HasColumnName("branchnum");
            entity.Property(e => e.Canclemoney)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("canclemoney");
            entity.Property(e => e.Cancletime)
                .HasColumnType("datetime")
                .HasColumnName("cancletime");
            entity.Property(e => e.Cardid)
                .HasMaxLength(30)
                .HasColumnName("cardid");
            entity.Property(e => e.Cardname)
                .HasMaxLength(30)
                .HasColumnName("cardname");
            entity.Property(e => e.Dealnum)
                .HasMaxLength(24)
                .HasColumnName("dealnum");
            entity.Property(e => e.Dealtime)
                .HasMaxLength(10)
                .HasColumnName("dealtime");
            entity.Property(e => e.Dendflag)
                .HasColumnType("smallint(6)")
                .HasColumnName("dendflag");
            entity.Property(e => e.Enddate).HasColumnName("enddate");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Intime)
                .HasColumnType("datetime")
                .HasColumnName("intime");
            entity.Property(e => e.Money)
                .HasColumnType("int(11)")
                .HasColumnName("money");
            entity.Property(e => e.Msg)
                .HasMaxLength(45)
                .HasDefaultValueSql("''")
                .HasColumnName("msg");
            entity.Property(e => e.Outdevicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("outdevicenum");
            entity.Property(e => e.Outtime)
                .HasColumnType("datetime")
                .HasColumnName("outtime");
            entity.Property(e => e.Parktime)
                .HasColumnType("int(11)")
                .HasColumnName("parktime");
            entity.Property(e => e.Parktype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("parktype");
            entity.Property(e => e.Pindex)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("pindex");
            entity.Property(e => e.Posid)
                .HasMaxLength(16)
                .HasColumnName("posid");
            entity.Property(e => e.Receiptnum)
                .HasMaxLength(8)
                .HasColumnName("receiptnum");
            entity.Property(e => e.Rescode)
                .HasMaxLength(6)
                .HasColumnName("rescode");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Tendflag)
                .HasColumnType("smallint(6)")
                .HasColumnName("tendflag");
            entity.Property(e => e.Termid)
                .HasMaxLength(16)
                .HasColumnName("termid");
            entity.Property(e => e.Ticketdata)
                .HasMaxLength(20)
                .HasColumnName("ticketdata");
        });

        modelBuilder.Entity<Tblackfee>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Carnum })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tblackfee");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Caldatetime)
                .HasMaxLength(45)
                .HasColumnName("caldatetime");
            entity.Property(e => e.Indatetime)
                .HasMaxLength(20)
                .HasColumnName("indatetime");
            entity.Property(e => e.Indevicenum)
                .HasColumnType("int(11)")
                .HasColumnName("indevicenum");
            entity.Property(e => e.Inimage)
                .HasMaxLength(80)
                .HasColumnName("inimage");
            entity.Property(e => e.Managerid)
                .HasColumnType("int(11)")
                .HasColumnName("managerid");
            entity.Property(e => e.Outdatetime)
                .HasMaxLength(20)
                .HasColumnName("outdatetime");
            entity.Property(e => e.Outdevicenum)
                .HasColumnType("int(11)")
                .HasColumnName("outdevicenum");
            entity.Property(e => e.Outimage)
                .HasMaxLength(80)
                .HasColumnName("outimage");
            entity.Property(e => e.Parkmoney)
                .HasColumnType("int(11)")
                .HasColumnName("parkmoney");
        });

        modelBuilder.Entity<Tblacklist>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Carnum })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tblacklist");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Indatetime)
                .HasMaxLength(20)
                .HasColumnName("indatetime");
            entity.Property(e => e.Indevicenum)
                .HasColumnType("int(11)")
                .HasColumnName("indevicenum");
            entity.Property(e => e.Inimage)
                .HasMaxLength(80)
                .HasColumnName("inimage");
            entity.Property(e => e.Outdatetime)
                .HasMaxLength(20)
                .HasColumnName("outdatetime");
            entity.Property(e => e.Outdevicenum)
                .HasColumnType("int(11)")
                .HasColumnName("outdevicenum");
            entity.Property(e => e.Outimage)
                .HasMaxLength(80)
                .HasColumnName("outimage");
            entity.Property(e => e.Parkmoney)
                .HasColumnType("int(11)")
                .HasColumnName("parkmoney");
        });

        modelBuilder.Entity<Tcompany>(entity =>
        {
            entity.HasKey(e => new { e.Groupnum, e.Companycode, e.Sitenum })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

            entity.ToTable("tcompany");

            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Companycode)
                .HasColumnType("smallint(6)")
                .HasColumnName("companycode");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Companyaddress)
                .HasMaxLength(100)
                .HasColumnName("companyaddress");
            entity.Property(e => e.Companyfax)
                .HasMaxLength(20)
                .HasColumnName("companyfax");
            entity.Property(e => e.Companyname)
                .HasMaxLength(40)
                .HasColumnName("companyname");
            entity.Property(e => e.Companytel)
                .HasMaxLength(20)
                .HasColumnName("companytel");
            entity.Property(e => e.Managercode)
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Note)
                .HasMaxLength(100)
                .HasColumnName("note");
            entity.Property(e => e.Pregidentname)
                .HasMaxLength(30)
                .HasColumnName("pregidentname");
        });

        modelBuilder.Entity<Tcuponinfo>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Recorddate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tcuponinfo");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Recorddate)
                .HasDefaultValueSql("'0000-00-00'")
                .HasColumnName("recorddate");
            entity.Property(e => e.Applyvalue)
                .HasColumnType("int(11)")
                .HasColumnName("applyvalue");
            entity.Property(e => e.Cuponkind)
                .HasColumnType("smallint(6)")
                .HasColumnName("cuponkind");
            entity.Property(e => e.Cuponnum)
                .HasColumnType("int(11)")
                .HasColumnName("cuponnum");
            entity.Property(e => e.Enddate).HasColumnName("enddate");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Managercode)
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Startdate).HasColumnName("startdate");
            entity.Property(e => e.Unitprice)
                .HasColumnType("int(11)")
                .HasColumnName("unitprice");
        });

        modelBuilder.Entity<Tdayclosecal>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tdayclosecal");

            entity.HasIndex(e => e.Edate, "xdaydate");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Creditnum)
                .HasColumnType("int(11)")
                .HasColumnName("creditnum");
            entity.Property(e => e.Creditprice)
                .HasColumnType("int(11)")
                .HasColumnName("creditprice");
            entity.Property(e => e.Device)
                .HasColumnType("smallint(6)")
                .HasColumnName("device");
            entity.Property(e => e.Devicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Edate).HasColumnName("edate");
            entity.Property(e => e.Etime)
                .HasColumnType("time")
                .HasColumnName("etime");
            entity.Property(e => e.Etype)
                .HasColumnType("smallint(6)")
                .HasColumnName("etype");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Mainclass)
                .HasColumnType("smallint(6)")
                .HasColumnName("mainclass");
            entity.Property(e => e.Manid)
                .HasMaxLength(20)
                .HasColumnName("manid");
            entity.Property(e => e.Midclass)
                .HasColumnType("smallint(6)")
                .HasColumnName("midclass");
            entity.Property(e => e.Parkmoney)
                .HasColumnType("int(11)")
                .HasColumnName("parkmoney");
            entity.Property(e => e.Parkprice)
                .HasColumnType("int(11)")
                .HasColumnName("parkprice");
            entity.Property(e => e.Salemoney)
                .HasColumnType("int(11)")
                .HasColumnName("salemoney");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Subclass)
                .HasColumnType("smallint(6)")
                .HasColumnName("subclass");
            entity.Property(e => e.Tmoneynum)
                .HasColumnType("int(11)")
                .HasColumnName("tmoneynum");
            entity.Property(e => e.Tmoneyprice)
                .HasColumnType("int(11)")
                .HasColumnName("tmoneyprice");
            entity.Property(e => e.Totalnum)
                .HasColumnType("int(11)")
                .HasColumnName("totalnum");
        });

        modelBuilder.Entity<Tdepartment>(entity =>
        {
            entity.HasKey(e => new { e.Sitenum, e.Groupnum, e.Companycode, e.Deptcode })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0, 0 });

            entity.ToTable("tdepartment");

            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Companycode)
                .HasColumnType("smallint(6)")
                .HasColumnName("companycode");
            entity.Property(e => e.Deptcode)
                .HasColumnType("int(11)")
                .HasColumnName("deptcode");
            entity.Property(e => e.Companyname)
                .HasMaxLength(40)
                .HasColumnName("companyname");
            entity.Property(e => e.Deptfax)
                .HasMaxLength(20)
                .HasColumnName("deptfax");
            entity.Property(e => e.Deptname)
                .HasMaxLength(30)
                .HasColumnName("deptname");
            entity.Property(e => e.Depttel)
                .HasMaxLength(20)
                .HasColumnName("depttel");
            entity.Property(e => e.Managercode)
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
        });

        modelBuilder.Entity<Tdevicealarm>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Alarmtime })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tdevicealarm");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Alarmtime)
                .HasDefaultValueSql("'0000-00-00 00:00:00'")
                .HasColumnType("datetime")
                .HasColumnName("alarmtime");
            entity.Property(e => e.Alarmkind)
                .HasMaxLength(40)
                .HasColumnName("alarmkind");
            entity.Property(e => e.Alarmmessage)
                .HasMaxLength(100)
                .HasColumnName("alarmmessage");
            entity.Property(e => e.Devicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Managercode)
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Repairmessage)
                .HasMaxLength(100)
                .HasColumnName("repairmessage");
            entity.Property(e => e.Repairtime)
                .HasColumnType("datetime")
                .HasColumnName("repairtime");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Tdevicelist>(entity =>
        {
            entity.HasKey(e => e.Ip).HasName("PRIMARY");

            entity.ToTable("tdevicelist");

            entity.Property(e => e.Ip)
                .HasMaxLength(20)
                .HasDefaultValueSql("'0.0.0.0'")
                .HasColumnName("ip");
            entity.Property(e => e.Connect)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("connect");
            entity.Property(e => e.Connecttime)
                .HasDefaultValueSql("'2014-01-01 00:00:00'")
                .HasColumnType("datetime")
                .HasColumnName("connecttime");
            entity.Property(e => e.Devicename)
                .HasMaxLength(20)
                .HasColumnName("devicename");
            entity.Property(e => e.Devicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Devicetype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("devicetype");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Income)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("income");
            entity.Property(e => e.Iotype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("iotype");
            entity.Property(e => e.Linkip)
                .HasMaxLength(20)
                .HasColumnName("linkip");
            entity.Property(e => e.Note)
                .HasMaxLength(100)
                .HasColumnName("note");
            entity.Property(e => e.Restmoney0)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney0");
            entity.Property(e => e.Restmoney1)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney1");
            entity.Property(e => e.Restmoney2)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney2");
            entity.Property(e => e.Restmoney3)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney3");
            entity.Property(e => e.Restmoney4)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney4");
            entity.Property(e => e.Restmoney5)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney5");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Tdevicestate>(entity =>
        {
            entity.HasKey(e => e.Ip).HasName("PRIMARY");

            entity.ToTable("tdevicestate");

            entity.Property(e => e.Ip)
                .HasMaxLength(20)
                .HasDefaultValueSql("'0.0.0.0'")
                .HasColumnName("ip");
            entity.Property(e => e.Connecttime)
                .HasColumnType("datetime")
                .HasColumnName("connecttime");
            entity.Property(e => e.Devicename)
                .HasMaxLength(20)
                .HasColumnName("devicename");
            entity.Property(e => e.Devicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Devicetype)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicetype");
            entity.Property(e => e.Errcode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("errcode");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Income)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("income");
            entity.Property(e => e.Lasttime)
                .HasColumnType("datetime")
                .HasColumnName("lasttime");
            entity.Property(e => e.Restmoney)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney");
            entity.Property(e => e.Restmoney0)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney0");
            entity.Property(e => e.Restmoney1)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney1");
            entity.Property(e => e.Restmoney2)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney2");
            entity.Property(e => e.Restmoney3)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney3");
            entity.Property(e => e.Restmoney4)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney4");
            entity.Property(e => e.Restmoney5)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("restmoney5");
            entity.Property(e => e.Salemoney)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("salemoney");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Tdiscount>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Key })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tdiscount");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Key)
                .HasColumnType("int(11)")
                .HasColumnName("key");
            entity.Property(e => e.Groupnum)
                .HasColumnType("int(11)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Maxcount)
                .HasColumnType("int(11)")
                .HasColumnName("maxcount");
            entity.Property(e => e.Mid)
                .HasMaxLength(20)
                .HasColumnName("mid");
            entity.Property(e => e.Moddate)
                .HasColumnType("datetime")
                .HasColumnName("moddate");
            entity.Property(e => e.Regdate)
                .HasColumnType("datetime")
                .HasColumnName("regdate");
            entity.Property(e => e.Sitenum)
                .HasColumnType("int(11)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Title)
                .HasMaxLength(45)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasColumnType("int(11)")
                .HasColumnName("type");
            entity.Property(e => e.Value)
                .HasColumnType("int(11)")
                .HasColumnName("value");
        });

        modelBuilder.Entity<Tdiscountaccount>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Id })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tdiscountaccount");

            entity.HasIndex(e => e.Id, "id_UNIQUE").IsUnique();

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .HasColumnName("id");
            entity.Property(e => e.Deptcode)
                .HasColumnType("int(11)")
                .HasColumnName("deptcode");
            entity.Property(e => e.Grade)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("grade");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(40)
                .HasDefaultValueSql("''")
                .HasColumnName("password");
            entity.Property(e => e.Regdate)
                .HasColumnType("datetime")
                .HasColumnName("regdate");
            entity.Property(e => e.Telnum)
                .HasMaxLength(30)
                .HasDefaultValueSql("''")
                .HasColumnName("telnum");
        });

        modelBuilder.Entity<Tdiscountdept>(entity =>
        {
            entity.HasKey(e => e.Deptcode).HasName("PRIMARY");

            entity.ToTable("tdiscountdept");

            entity.Property(e => e.Deptcode)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("deptcode");
            entity.Property(e => e.Carcount)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("carcount");
            entity.Property(e => e.Carmaxnum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("carmaxnum");
            entity.Property(e => e.Deptname)
                .HasMaxLength(45)
                .HasColumnName("deptname");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Regdate)
                .HasColumnType("datetime")
                .HasColumnName("regdate");
            entity.Property(e => e.Salecount)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("salecount");
            entity.Property(e => e.Salemaxnum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("salemaxnum");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Tdiscountinfo>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Sdate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tdiscountinfo");

            entity.HasIndex(e => e.Tparkindex, "tparkindex_idx");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Sdate)
                .HasColumnType("datetime")
                .HasColumnName("sdate");
            entity.Property(e => e.Carnum)
                .HasMaxLength(30)
                .HasColumnName("carnum");
            entity.Property(e => e.Deptcode)
                .HasColumnType("int(11)")
                .HasColumnName("deptcode");
            entity.Property(e => e.Devicenum)
                .HasColumnType("int(11)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Indate)
                .HasColumnType("datetime")
                .HasColumnName("indate");
            entity.Property(e => e.Logid)
                .HasMaxLength(20)
                .HasColumnName("logid");
            entity.Property(e => e.Remoteip)
                .HasMaxLength(30)
                .HasColumnName("remoteip");
            entity.Property(e => e.Salecode)
                .HasColumnType("int(11)")
                .HasColumnName("salecode");
            entity.Property(e => e.Salemoney)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("salemoney");
            entity.Property(e => e.Saletype)
                .HasColumnType("int(11)")
                .HasColumnName("saletype");
            entity.Property(e => e.Salevalue)
                .HasColumnType("int(11)")
                .HasColumnName("salevalue");
            entity.Property(e => e.Tparkindex)
                .HasColumnType("int(11)")
                .HasColumnName("tparkindex");
        });

        modelBuilder.Entity<Tdiscountissue>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Issuedate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tdiscountissue");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Issuedate)
                .HasDefaultValueSql("'2010-10-11'")
                .HasColumnName("issuedate");
            entity.Property(e => e.Endnum)
                .HasColumnType("int(11)")
                .HasColumnName("endnum");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Issuedept)
                .HasMaxLength(20)
                .HasColumnName("issuedept");
            entity.Property(e => e.Issuenote)
                .HasMaxLength(30)
                .HasColumnName("issuenote");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(20)
                .HasColumnName("managername");
            entity.Property(e => e.Paymenttype)
                .HasMaxLength(20)
                .HasColumnName("paymenttype");
            entity.Property(e => e.Price)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("price");
            entity.Property(e => e.Saletype)
                .HasColumnType("smallint(6)")
                .HasColumnName("saletype");
            entity.Property(e => e.Saleval)
                .HasColumnType("int(11)")
                .HasColumnName("saleval");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Startnum)
                .HasColumnType("int(11)")
                .HasColumnName("startnum");
        });

        modelBuilder.Entity<Tdiscountreport>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity
                .ToTable("tdiscountreport", tb => tb.HasComment("할인권 매출"))
                .HasCharSet("utf8mb4")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.Deptcode, "idx_tdiscountreport_deptcode");

            entity.HasIndex(e => e.Id, "idx_tdiscountreport_id");

            entity.HasIndex(e => e.Purchasedate, "idx_tdiscountreport_purchasedate");

            entity.HasIndex(e => e.Salecode, "idx_tdiscountreport_salecode");

            entity.Property(e => e.Xindex)
                .HasComment("PK")
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Amount)
                .HasComment("구매금액(원) = quantity * saleprice 스냅샷")
                .HasColumnType("int(11)")
                .HasColumnName("amount");
            entity.Property(e => e.Deptcode)
                .HasComment("매장코드 (tdiscountdept.deptcode처럼 사용)")
                .HasColumnType("int(11)")
                .HasColumnName("deptcode");
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasComment("구매자/계정 id (tdiscountaccount.id처럼 사용)")
                .HasColumnName("id");
            entity.Property(e => e.Purchasedate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("구매일자")
                .HasColumnType("datetime")
                .HasColumnName("purchasedate");
            entity.Property(e => e.Quantity)
                .HasComment("매수(장)")
                .HasColumnType("int(11)")
                .HasColumnName("quantity");
            entity.Property(e => e.Salecode)
                .HasComment("할인종류 (tdiscounttable.salecode처럼 사용)")
                .HasColumnType("int(11)")
                .HasColumnName("salecode");
        });

        modelBuilder.Entity<Tdiscountsale>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tdiscountsale");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Dendflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("dendflag");
            entity.Property(e => e.Managercode)
                .HasColumnType("int(11)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Manflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("manflag");
            entity.Property(e => e.Saleamount)
                .HasColumnType("int(11)")
                .HasColumnName("saleamount");
            entity.Property(e => e.Salecode)
                .HasColumnType("int(11)")
                .HasColumnName("salecode");
            entity.Property(e => e.Saledate)
                .HasColumnType("datetime")
                .HasColumnName("saledate");
            entity.Property(e => e.Salemoney)
                .HasColumnType("int(11)")
                .HasColumnName("salemoney");
            entity.Property(e => e.Saleshop)
                .HasMaxLength(30)
                .HasColumnName("saleshop");
            entity.Property(e => e.Saletitle)
                .HasMaxLength(30)
                .HasColumnName("saletitle");
        });

        modelBuilder.Entity<Tdiscounttable>(entity =>
        {
            entity.HasKey(e => e.Salecode).HasName("PRIMARY");

            entity.ToTable("tdiscounttable");

            entity.HasIndex(e => e.Salecode, "salecode_UNIQUE").IsUnique();

            entity.Property(e => e.Salecode)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("salecode");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Limitval)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("limitval");
            entity.Property(e => e.Regdate)
                .HasColumnType("datetime")
                .HasColumnName("regdate");
            entity.Property(e => e.Saletitle)
                .HasMaxLength(30)
                .HasDefaultValueSql("''")
                .HasColumnName("saletitle");
            entity.Property(e => e.Saletype)
                .HasColumnType("int(11)")
                .HasColumnName("saletype");
            entity.Property(e => e.Salevalue)
                .HasColumnType("int(11)")
                .HasColumnName("salevalue");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Ticketon)
                .HasColumnType("int(11)")
                .HasColumnName("ticketon");
        });

        modelBuilder.Entity<Tdisperson>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Carnum })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tdisperson");

            entity.HasIndex(e => e.Carnum, "carnum_UNIQUE").IsUnique();

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Disid)
                .HasMaxLength(40)
                .HasColumnName("disid");
            entity.Property(e => e.Enddate)
                .HasDefaultValueSql("'2018-01-01'")
                .HasColumnName("enddate");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
            entity.Property(e => e.Recorddate)
                .HasDefaultValueSql("'2018-01-01'")
                .HasColumnName("recorddate");
            entity.Property(e => e.Salekey)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("salekey");
            entity.Property(e => e.Salemsg)
                .HasMaxLength(45)
                .HasColumnName("salemsg");
            entity.Property(e => e.Saletype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("saletype");
            entity.Property(e => e.Saleval)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("saleval");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Startdate)
                .HasDefaultValueSql("'2018-01-01'")
                .HasColumnName("startdate");
            entity.Property(e => e.Telnum)
                .HasMaxLength(30)
                .HasColumnName("telnum");
        });

        modelBuilder.Entity<Tdisperson2>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Carnum })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tdisperson2");

            entity.HasIndex(e => e.Carnum, "carnum_UNIQUE").IsUnique();

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Disid)
                .HasMaxLength(40)
                .HasColumnName("disid");
            entity.Property(e => e.Enddate)
                .HasDefaultValueSql("'2019-01-01'")
                .HasColumnName("enddate");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
            entity.Property(e => e.Parkday)
                .HasMaxLength(12)
                .HasColumnName("parkday");
            entity.Property(e => e.Recorddate)
                .HasDefaultValueSql("'2019-01-01'")
                .HasColumnName("recorddate");
            entity.Property(e => e.Saleday).HasColumnName("saleday");
            entity.Property(e => e.Salekey)
                .HasColumnType("int(11)")
                .HasColumnName("salekey");
            entity.Property(e => e.Salemsg)
                .HasMaxLength(30)
                .HasColumnName("salemsg");
            entity.Property(e => e.Saletype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("saletype");
            entity.Property(e => e.Saleval)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("saleval");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Startdate)
                .HasDefaultValueSql("'2019-01-01'")
                .HasColumnName("startdate");
            entity.Property(e => e.Telnum)
                .HasMaxLength(30)
                .HasColumnName("telnum");
        });

        modelBuilder.Entity<Tgateinfo>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Opdate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tgateinfo");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Opdate)
                .HasDefaultValueSql("'2010-09-15'")
                .HasColumnName("opdate");
            entity.Property(e => e.Dendflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("dendflag");
            entity.Property(e => e.Devicenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Gatename)
                .HasMaxLength(20)
                .HasColumnName("gatename");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Inouttype)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("inouttype");
            entity.Property(e => e.Ip)
                .HasMaxLength(20)
                .HasColumnName("ip");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasDefaultValueSql("'0'")
                .HasColumnName("managername");
            entity.Property(e => e.Manflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("manflag");
            entity.Property(e => e.Openclose)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("openclose");
            entity.Property(e => e.Ophour)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("ophour");
            entity.Property(e => e.Opmin)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("opmin");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Tendflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("tendflag");
        });

        modelBuilder.Entity<Tholiday>(entity =>
        {
            entity.HasKey(e => e.Hdate).HasName("PRIMARY");

            entity.ToTable("tholiday");

            entity.HasIndex(e => e.Hdate, "hdate_UNIQUE").IsUnique();

            entity.Property(e => e.Hdate).HasColumnName("hdate");
            entity.Property(e => e.Hname)
                .HasMaxLength(30)
                .HasColumnName("hname");
        });

        modelBuilder.Entity<Tjsalecode>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Salecode })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tjsalecode");

            entity.HasIndex(e => e.Xindex, "hdate_UNIQUE").IsUnique();

            entity.HasIndex(e => e.Salecode, "salecode_UNIQUE").IsUnique();

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Salecode)
                .HasColumnType("int(11)")
                .HasColumnName("salecode");
        });

        modelBuilder.Entity<Tlogon>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tlogon");

            entity.HasIndex(e => e.Logdatetime, "xlogdate");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Devicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Devicetype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("devicetype");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Logdatetime)
                .HasColumnType("datetime")
                .HasColumnName("logdatetime");
            entity.Property(e => e.Logtype)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("logtype");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managerid)
                .HasMaxLength(16)
                .HasColumnName("managerid");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Managerpass)
                .HasMaxLength(40)
                .HasColumnName("managerpass");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Tlprconfig>(entity =>
        {
            entity.HasKey(e => e.Lprip).HasName("PRIMARY");

            entity.ToTable("tlprconfig");

            entity.Property(e => e.Lprip)
                .HasMaxLength(24)
                .HasDefaultValueSql("'0.0.0.0'")
                .HasColumnName("lprip");
            entity.Property(e => e.Address)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("address");
            entity.Property(e => e.Aptcall)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("aptcall");
            entity.Property(e => e.Comuse)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("comuse");
            entity.Property(e => e.Connection)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("connection");
            entity.Property(e => e.Iotype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("iotype");
            entity.Property(e => e.Ldmport)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("ldmport");
            entity.Property(e => e.Name)
                .HasMaxLength(45)
                .HasColumnName("name");
            entity.Property(e => e.Parkarea)
                .HasMaxLength(2)
                .HasDefaultValueSql("''")
                .HasColumnName("parkarea");
            entity.Property(e => e.Relaynum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("relaynum");
            entity.Property(e => e.Relayport)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("relayport");
            entity.Property(e => e.Savedata)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("savedata");
            entity.Property(e => e.Tdport)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("tdport");
        });

        modelBuilder.Entity<Tmanager>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tmanager");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Devicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Managercode)
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managerid)
                .HasMaxLength(16)
                .HasColumnName("managerid");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Managerplace)
                .HasMaxLength(30)
                .HasColumnName("managerplace");
            entity.Property(e => e.Managerpower)
                .HasColumnType("smallint(6)")
                .HasColumnName("managerpower");
            entity.Property(e => e.Managerpw)
                .HasMaxLength(40)
                .HasColumnName("managerpw");
            entity.Property(e => e.Managertel)
                .HasMaxLength(20)
                .HasColumnName("managertel");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Workingend)
                .HasColumnType("time")
                .HasColumnName("workingend");
            entity.Property(e => e.Workingstart)
                .HasColumnType("time")
                .HasColumnName("workingstart");
        });

        modelBuilder.Entity<Tmisuinfo>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tmisuinfo");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Carnum)
                .HasMaxLength(30)
                .HasColumnName("carnum");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Indate)
                .HasColumnType("datetime")
                .HasColumnName("indate");
            entity.Property(e => e.Misu)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("misu");
            entity.Property(e => e.Outdate)
                .HasColumnType("datetime")
                .HasColumnName("outdate");
            entity.Property(e => e.Parkmoney)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("parkmoney");
            entity.Property(e => e.Pindex)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("pindex");
            entity.Property(e => e.Salemoney)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("salemoney");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Tmoneyerrlog>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tmoneyerrlog");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Errdate).HasColumnName("errdate");
            entity.Property(e => e.Errlog)
                .HasMaxLength(50)
                .HasColumnName("errlog");
            entity.Property(e => e.Filename)
                .HasMaxLength(80)
                .HasColumnName("filename");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Tnocar>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Iodate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tnocar");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Iodate)
                .HasMaxLength(24)
                .HasColumnName("iodate");
            entity.Property(e => e.Image)
                .HasMaxLength(100)
                .HasColumnName("image");
            entity.Property(e => e.Ioname)
                .HasMaxLength(45)
                .HasColumnName("ioname");
            entity.Property(e => e.Ionum)
                .HasColumnType("int(11)")
                .HasColumnName("ionum");
            entity.Property(e => e.Outflag)
                .HasColumnType("int(11)")
                .HasColumnName("outflag");
        });

        modelBuilder.Entity<Tnocarnum>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tnocarnum");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Devicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Imgname)
                .HasMaxLength(80)
                .HasColumnName("imgname");
            entity.Property(e => e.Indate)
                .HasColumnType("datetime")
                .HasColumnName("indate");
            entity.Property(e => e.Outflag)
                .HasDefaultValueSql("'73'")
                .HasColumnType("int(11)")
                .HasColumnName("outflag");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Ticketnum)
                .HasColumnType("int(11)")
                .HasColumnName("ticketnum");
        });

        modelBuilder.Entity<Tocssale>(entity =>
        {
            entity.HasKey(e => e.Dccd).HasName("PRIMARY");

            entity.ToTable("tocssale");

            entity.Property(e => e.Dccd)
                .ValueGeneratedNever()
                .HasColumnType("smallint(6)")
                .HasColumnName("dccd");
            entity.Property(e => e.Saletype)
                .HasColumnType("smallint(6)")
                .HasColumnName("saletype");
            entity.Property(e => e.Saleval)
                .HasColumnType("int(11)")
                .HasColumnName("saleval");
        });

        modelBuilder.Entity<Topclosecal>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("topclosecal");

            entity.HasIndex(e => e.Edate, "xopdate");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Creditnum)
                .HasColumnType("int(11)")
                .HasColumnName("creditnum");
            entity.Property(e => e.Creditprice)
                .HasColumnType("int(11)")
                .HasColumnName("creditprice");
            entity.Property(e => e.Device)
                .HasColumnType("smallint(6)")
                .HasColumnName("device");
            entity.Property(e => e.Devicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Edate).HasColumnName("edate");
            entity.Property(e => e.Etime)
                .HasColumnType("time")
                .HasColumnName("etime");
            entity.Property(e => e.Etype)
                .HasColumnType("smallint(6)")
                .HasColumnName("etype");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Mainclass)
                .HasColumnType("smallint(6)")
                .HasColumnName("mainclass");
            entity.Property(e => e.Manid)
                .HasMaxLength(20)
                .HasColumnName("manid");
            entity.Property(e => e.Midclass)
                .HasColumnType("smallint(6)")
                .HasColumnName("midclass");
            entity.Property(e => e.Parkmoney)
                .HasColumnType("int(11)")
                .HasColumnName("parkmoney");
            entity.Property(e => e.Parkprice)
                .HasColumnType("int(11)")
                .HasColumnName("parkprice");
            entity.Property(e => e.Salemoney)
                .HasColumnType("int(11)")
                .HasColumnName("salemoney");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Subclass)
                .HasColumnType("smallint(6)")
                .HasColumnName("subclass");
            entity.Property(e => e.Tmoneynum)
                .HasColumnType("int(11)")
                .HasColumnName("tmoneynum");
            entity.Property(e => e.Tmoneyprice)
                .HasColumnType("int(11)")
                .HasColumnName("tmoneyprice");
            entity.Property(e => e.Totalnum)
                .HasColumnType("int(11)")
                .HasColumnName("totalnum");
        });

        modelBuilder.Entity<Tparkconfig>(entity =>
        {
            entity.HasKey(e => e.Sitenum).HasName("PRIMARY");

            entity.ToTable("tparkconfig");

            entity.HasIndex(e => e.Sitenum, "site_UNIQUE").IsUnique();

            entity.Property(e => e.Sitenum)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Xfile)
                .HasColumnType("blob")
                .HasColumnName("xfile");
        });

        modelBuilder.Entity<Tparkemergency>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Indate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tparkemergency");

            entity.HasIndex(e => e.Carnum, "xcarnum");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Indate)
                .HasColumnType("datetime")
                .HasColumnName("indate");
            entity.Property(e => e.Carnum)
                .HasMaxLength(45)
                .HasDefaultValueSql("'0'")
                .HasColumnName("carnum");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Indevicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("indevicenum");
            entity.Property(e => e.Inimage)
                .HasMaxLength(100)
                .HasDefaultValueSql("''")
                .HasColumnName("inimage");
            entity.Property(e => e.Msg)
                .HasMaxLength(100)
                .HasColumnName("msg");
            entity.Property(e => e.Outdate)
                .HasColumnType("datetime")
                .HasColumnName("outdate");
            entity.Property(e => e.Outdevicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("outdevicenum");
            entity.Property(e => e.Outflag)
                .HasDefaultValueSql("'73'")
                .HasColumnType("smallint(6)")
                .HasColumnName("outflag");
            entity.Property(e => e.Outimage)
                .HasMaxLength(100)
                .HasDefaultValueSql("''")
                .HasColumnName("outimage");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Tparkfee>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tparkfee");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Cartype)
                .HasColumnType("int(11)")
                .HasColumnName("cartype");
            entity.Property(e => e.Dayshift)
                .HasColumnType("int(11)")
                .HasColumnName("dayshift");
            entity.Property(e => e.Feestep)
                .HasColumnType("int(11)")
                .HasColumnName("feestep");
            entity.Property(e => e.Groupnum)
                .HasColumnType("int(11)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Maxcount)
                .HasColumnType("int(11)")
                .HasColumnName("maxcount");
            entity.Property(e => e.Mid)
                .HasMaxLength(20)
                .HasColumnName("mid");
            entity.Property(e => e.Moddate)
                .HasColumnType("datetime")
                .HasColumnName("moddate");
            entity.Property(e => e.Parkfee)
                .HasColumnType("int(11)")
                .HasColumnName("parkfee");
            entity.Property(e => e.Parktime)
                .HasColumnType("int(11)")
                .HasColumnName("parktime");
            entity.Property(e => e.Regdate)
                .HasColumnType("datetime")
                .HasColumnName("regdate");
            entity.Property(e => e.Sitenum)
                .HasColumnType("int(11)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Weektype)
                .HasColumnType("int(11)")
                .HasColumnName("weektype");
        });

        modelBuilder.Entity<Tparkin>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Indate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tparkin");

            entity.HasIndex(e => e.Carnum, "xcarnum");

            entity.HasIndex(e => e.Ticketdata, "xticketdata");

            entity.HasIndex(e => e.Ticketnum, "xticketnum");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Indate)
                .HasDefaultValueSql("'2010-09-15'")
                .HasColumnName("indate");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Indevicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("indevicenum");
            entity.Property(e => e.Inhour)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("inhour");
            entity.Property(e => e.Inimage)
                .HasMaxLength(80)
                .HasColumnName("inimage");
            entity.Property(e => e.Inmin)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("inmin");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Outflag)
                .HasDefaultValueSql("'73'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("outflag");
            entity.Property(e => e.Parkcartype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("parkcartype");
            entity.Property(e => e.Parkonplace)
                .HasMaxLength(20)
                .HasColumnName("parkonplace");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Ticketcartype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("ticketcartype");
            entity.Property(e => e.Ticketdata)
                .HasMaxLength(30)
                .HasColumnName("ticketdata");
            entity.Property(e => e.Ticketnum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("ticketnum");
        });

        modelBuilder.Entity<Tparkinfo>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Outdate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tparkinfo");

            entity.HasIndex(e => e.Carnum, "xcarnum");

            entity.HasIndex(e => e.Ticketdata, "xticketdata");

            entity.HasIndex(e => e.Ticketnum, "xticketnum");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Outdate)
                .HasDefaultValueSql("'2010-01-01'")
                .HasColumnName("outdate");
            entity.Property(e => e.Acceptno)
                .HasMaxLength(20)
                .HasColumnName("acceptno");
            entity.Property(e => e.Accepttime).HasColumnName("accepttime");
            entity.Property(e => e.Backimage)
                .HasMaxLength(80)
                .HasColumnName("backimage");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasDefaultValueSql("''")
                .HasColumnName("carnum");
            entity.Property(e => e.Creditcardno)
                .HasMaxLength(50)
                .HasColumnName("creditcardno");
            entity.Property(e => e.Creditmoney)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("creditmoney");
            entity.Property(e => e.Credittype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("credittype");
            entity.Property(e => e.Denddate).HasColumnName("denddate");
            entity.Property(e => e.Dendflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("dendflag");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Indate).HasColumnName("indate");
            entity.Property(e => e.Indevicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("indevicenum");
            entity.Property(e => e.Inhour)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("inhour");
            entity.Property(e => e.Inimage)
                .HasMaxLength(80)
                .HasColumnName("inimage");
            entity.Property(e => e.Inmin)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("inmin");
            entity.Property(e => e.Intick)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("intick");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Manflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("manflag");
            entity.Property(e => e.Ocssalecode)
                .HasMaxLength(20)
                .HasColumnName("ocssalecode");
            entity.Property(e => e.Ocssaletype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("ocssaletype");
            entity.Property(e => e.Ocssaleval)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("ocssaleval");
            entity.Property(e => e.Outdevicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("outdevicenum");
            entity.Property(e => e.Outflag)
                .HasDefaultValueSql("'73'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("outflag");
            entity.Property(e => e.Outhour)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("outhour");
            entity.Property(e => e.Outimage)
                .HasMaxLength(80)
                .HasColumnName("outimage");
            entity.Property(e => e.Outmin)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("outmin");
            entity.Property(e => e.Outtick)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("outtick");
            entity.Property(e => e.Parkcaltype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("parkcaltype");
            entity.Property(e => e.Parkcartype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("parkcartype");
            entity.Property(e => e.Parkmoney)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("parkmoney");
            entity.Property(e => e.Parktime)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("parktime");
            entity.Property(e => e.Receiptnum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("receiptnum");
            entity.Property(e => e.Saleimage)
                .HasMaxLength(80)
                .HasColumnName("saleimage");
            entity.Property(e => e.Salemoney)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("salemoney");
            entity.Property(e => e.Salepercent)
                .HasColumnType("int(11)")
                .HasColumnName("salepercent");
            entity.Property(e => e.Saletime)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("saletime");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Tenddate).HasColumnName("tenddate");
            entity.Property(e => e.Tendflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("tendflag");
            entity.Property(e => e.Ticketcartype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("ticketcartype");
            entity.Property(e => e.Ticketdata)
                .HasMaxLength(30)
                .HasColumnName("ticketdata");
            entity.Property(e => e.Ticketnum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("ticketnum");
        });

        modelBuilder.Entity<Tparkingname>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tparkingname");

            entity.HasIndex(e => e.Groupnum, "KEYCmd_Type_UNIQUE").IsUnique();

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Groupnum)
                .HasColumnType("int(11)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Name)
                .HasMaxLength(60)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Tparknum>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tparknum");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Devicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Devicetype)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicetype");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Parkfullnum)
                .HasDefaultValueSql("'0000000000'")
                .HasColumnType("int(10) unsigned zerofill")
                .HasColumnName("parkfullnum");
            entity.Property(e => e.Parkinnum)
                .HasDefaultValueSql("'0000000000'")
                .HasColumnType("int(10) unsigned zerofill")
                .HasColumnName("parkinnum");
            entity.Property(e => e.Parkoutnum)
                .HasDefaultValueSql("'0000000000'")
                .HasColumnType("int(10) unsigned zerofill")
                .HasColumnName("parkoutnum");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Terminnum)
                .HasDefaultValueSql("'0000000000'")
                .HasColumnType("int(10) unsigned zerofill")
                .HasColumnName("terminnum");
            entity.Property(e => e.Termoutnum)
                .HasDefaultValueSql("'0000000000'")
                .HasColumnType("int(10) unsigned zerofill")
                .HasColumnName("termoutnum");
        });

        modelBuilder.Entity<Tparktitle>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tparktitle");

            entity.Property(e => e.Xindex)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Title)
                .HasMaxLength(40)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Tparkvaliable>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Cmd_Type })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tparkvaliable");

            entity.HasIndex(e => e.Cmd_Type, "Cmd_Type_UNIQUE").IsUnique();

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Cmd_Type)
                .HasMaxLength(30)
                .HasDefaultValueSql("''")
                .HasColumnName("Cmd_Type");
            entity.Property(e => e.Msg)
                .HasMaxLength(120)
                .HasDefaultValueSql("''")
                .HasColumnName("msg");
            entity.Property(e => e.Opt)
                .HasMaxLength(30)
                .HasDefaultValueSql("''")
                .HasColumnName("opt");
            entity.Property(e => e.Regdate)
                .HasColumnType("datetime")
                .HasColumnName("regdate");
            entity.Property(e => e.Val)
                .HasMaxLength(30)
                .HasDefaultValueSql("''")
                .HasColumnName("val");
        });

        modelBuilder.Entity<Tparkvariable>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tparkvariable");

            entity.HasIndex(e => new { e.Cmd_Type, e.Sitenum, e.Groupnum }, "Cmd_Type_UNIQUE").IsUnique();

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Cmd_Type)
                .HasMaxLength(35)
                .HasColumnName("Cmd_Type");
            entity.Property(e => e.Groupnum)
                .HasColumnType("int(11)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Msg)
                .HasMaxLength(120)
                .HasColumnName("msg");
            entity.Property(e => e.Opt)
                .HasMaxLength(35)
                .HasColumnName("opt");
            entity.Property(e => e.Regdate)
                .HasColumnType("datetime")
                .HasColumnName("regdate");
            entity.Property(e => e.Sitenum)
                .HasColumnType("int(11)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Val)
                .HasMaxLength(35)
                .HasColumnName("val");
        });

        modelBuilder.Entity<Tperiodaccount>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tperiodaccount");

            entity.HasIndex(e => e.Cardid, "xcardid");

            entity.HasIndex(e => e.Carnum, "xcarnum");

            entity.HasIndex(e => e.Recorddate, "xrecorddate");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Cardid)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("cardid");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Company1)
                .HasMaxLength(80)
                .HasColumnName("company1");
            entity.Property(e => e.Company2)
                .HasMaxLength(80)
                .HasColumnName("company2");
            entity.Property(e => e.Dendflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("dendflag");
            entity.Property(e => e.Devicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Devicetype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("devicetype");
            entity.Property(e => e.Enddate).HasColumnName("enddate");
            entity.Property(e => e.Endhour)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("endhour");
            entity.Property(e => e.Endmin)
                .HasColumnType("smallint(6)")
                .HasColumnName("endmin");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Manflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("manflag");
            entity.Property(e => e.Memo)
                .HasMaxLength(50)
                .HasColumnName("memo");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
            entity.Property(e => e.Paytype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("paytype");
            entity.Property(e => e.Price)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("price");
            entity.Property(e => e.Printflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("printflag");
            entity.Property(e => e.Recorddate)
                .HasDefaultValueSql("'2010-09-15'")
                .HasColumnName("recorddate");
            entity.Property(e => e.Salemsg)
                .HasMaxLength(40)
                .HasDefaultValueSql("''")
                .HasColumnName("salemsg");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Startdate).HasColumnName("startdate");
            entity.Property(e => e.Starthour)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("starthour");
            entity.Property(e => e.Startmin)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("startmin");
        });

        modelBuilder.Entity<Tperiodfee>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tperiodfee");

            entity.HasIndex(e => e.Xindex, "xindex_UNIQUE").IsUnique();

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Carnum)
                .HasMaxLength(25)
                .HasDefaultValueSql("''")
                .HasColumnName("carnum");
            entity.Property(e => e.Collection)
                .HasColumnType("smallint(6)")
                .HasColumnName("collection");
            entity.Property(e => e.Indate)
                .HasDefaultValueSql("'2013-01-01 00:00:00'")
                .HasColumnType("datetime")
                .HasColumnName("indate");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.Outdate)
                .HasDefaultValueSql("'2013-01-01 00:00:00'")
                .HasColumnType("datetime")
                .HasColumnName("outdate");
            entity.Property(e => e.Parkmoney)
                .HasColumnType("int(11)")
                .HasColumnName("parkmoney");
            entity.Property(e => e.Parktime)
                .HasColumnType("int(11)")
                .HasColumnName("parktime");
            entity.Property(e => e.Parktype)
                .HasColumnType("smallint(6)")
                .HasColumnName("parktype");
            entity.Property(e => e.Telnum)
                .HasMaxLength(20)
                .HasDefaultValueSql("''")
                .HasColumnName("telnum");
        });

        modelBuilder.Entity<Tperiodin>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Indate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tperiodin");

            entity.HasIndex(e => e.Cardid, "xcardid");

            entity.HasIndex(e => e.Carnum, "xcarnum");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Indate)
                .HasDefaultValueSql("'2010-09-15'")
                .HasColumnName("indate");
            entity.Property(e => e.Cardid)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("cardid");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Cartype)
                .HasMaxLength(30)
                .HasColumnName("cartype");
            entity.Property(e => e.Enddate).HasColumnName("enddate");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Indevicenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("indevicenum");
            entity.Property(e => e.Inhour)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("inhour");
            entity.Property(e => e.Inimage)
                .HasMaxLength(80)
                .HasColumnName("inimage");
            entity.Property(e => e.Inmin)
                .HasColumnType("smallint(6)")
                .HasColumnName("inmin");
            entity.Property(e => e.Intimetick)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("intimetick");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'99'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
            entity.Property(e => e.Outflag)
                .HasDefaultValueSql("'73'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("outflag");
            entity.Property(e => e.Parkonplace)
                .HasMaxLength(20)
                .HasColumnName("parkonplace");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Tperiodinout>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Outdate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tperiodinout");

            entity.HasIndex(e => e.Cardid, "xcardid");

            entity.HasIndex(e => e.Carnum, "xcarnum");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Outdate)
                .HasDefaultValueSql("'2010-01-01'")
                .HasColumnName("outdate");
            entity.Property(e => e.Backimage)
                .HasMaxLength(80)
                .HasColumnName("backimage");
            entity.Property(e => e.Cardid)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("cardid");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Cartype)
                .HasMaxLength(30)
                .HasColumnName("cartype");
            entity.Property(e => e.Enddate).HasColumnName("enddate");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Indate)
                .HasDefaultValueSql("'2010-09-15'")
                .HasColumnName("indate");
            entity.Property(e => e.Indevicenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("indevicenum");
            entity.Property(e => e.Inhour)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("inhour");
            entity.Property(e => e.Inimage)
                .HasMaxLength(80)
                .HasDefaultValueSql("'0'")
                .HasColumnName("inimage");
            entity.Property(e => e.Inmin)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("inmin");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
            entity.Property(e => e.Note)
                .HasMaxLength(45)
                .HasColumnName("note");
            entity.Property(e => e.Outdevicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("outdevicenum");
            entity.Property(e => e.Outflag)
                .HasDefaultValueSql("'73'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("outflag");
            entity.Property(e => e.Outhour)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("outhour");
            entity.Property(e => e.Outimage)
                .HasMaxLength(80)
                .HasColumnName("outimage");
            entity.Property(e => e.Outmin)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("outmin");
            entity.Property(e => e.Parktime)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("parktime");
            entity.Property(e => e.Parktimecode)
                .HasColumnType("smallint(6)")
                .HasColumnName("parktimecode");
            entity.Property(e => e.Parktimetime)
                .HasMaxLength(20)
                .HasColumnName("parktimetime");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
        });

        modelBuilder.Entity<Tperiodmember>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Carnum1 })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tperiodmember");

            entity.HasIndex(e => e.Cardid, "xcardid");

            entity.HasIndex(e => e.Carnum2, "xcarnum2");

            entity.HasIndex(e => e.Serialno, "xserialno");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Carnum1)
                .HasMaxLength(20)
                .HasDefaultValueSql("''")
                .HasColumnName("carnum1");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .HasDefaultValueSql("''")
                .HasColumnName("address");
            entity.Property(e => e.Antiflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("antiflag");
            entity.Property(e => e.Cardid)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("cardid");
            entity.Property(e => e.Carnum2)
                .HasMaxLength(20)
                .HasDefaultValueSql("''")
                .HasColumnName("carnum2");
            entity.Property(e => e.Cartype1)
                .HasMaxLength(30)
                .HasDefaultValueSql("''")
                .HasColumnName("cartype1");
            entity.Property(e => e.Cartype2)
                .HasMaxLength(30)
                .HasDefaultValueSql("''")
                .HasColumnName("cartype2");
            entity.Property(e => e.Company1)
                .HasMaxLength(80)
                .HasDefaultValueSql("''")
                .HasColumnName("company1");
            entity.Property(e => e.Company2)
                .HasMaxLength(80)
                .HasDefaultValueSql("''")
                .HasColumnName("company2");
            entity.Property(e => e.Devicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Enddate).HasColumnName("enddate");
            entity.Property(e => e.Groupcode)
                .HasMaxLength(20)
                .HasColumnName("groupcode");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Intimetick)
                .HasColumnType("int(11)")
                .HasColumnName("intimetick");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
            entity.Property(e => e.Note)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("note");
            entity.Property(e => e.Outflag)
                .HasDefaultValueSql("'73'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("outflag");
            entity.Property(e => e.Parkarea)
                .HasMaxLength(8)
                .HasColumnName("parkarea");
            entity.Property(e => e.Parklevel)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("parklevel");
            entity.Property(e => e.Parkprice)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("parkprice");
            entity.Property(e => e.Parktimecode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("parktimecode");
            entity.Property(e => e.Parktimetime)
                .HasMaxLength(14)
                .HasColumnName("parktimetime");
            entity.Property(e => e.Parktype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("parktype");
            entity.Property(e => e.Parkvalidday)
                .HasMaxLength(8)
                .HasColumnName("parkvalidday");
            entity.Property(e => e.Paytype)
                .HasMaxLength(45)
                .HasColumnName("paytype");
            entity.Property(e => e.Periodtype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("periodtype");
            entity.Property(e => e.Recorddate).HasColumnName("recorddate");
            entity.Property(e => e.Reserved1)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("reserved1");
            entity.Property(e => e.Reserved2)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("reserved2");
            entity.Property(e => e.Reserved3)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("reserved3");
            entity.Property(e => e.Reserved4)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("reserved4");
            entity.Property(e => e.Serialno)
                .HasMaxLength(20)
                .HasColumnName("serialno");
            entity.Property(e => e.Serviceday)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("serviceday");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Startdate).HasColumnName("startdate");
            entity.Property(e => e.Telnum)
                .HasMaxLength(20)
                .HasColumnName("telnum");
            entity.Property(e => e.Useflag)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("useflag");
        });

        modelBuilder.Entity<Tperiodparktime>(entity =>
        {
            entity.HasKey(e => new { e.Sitenum, e.Groupnum, e.Timecode })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

            entity.ToTable("tperiodparktime");

            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Timecode)
                .HasColumnType("smallint(6)")
                .HasColumnName("timecode");
            entity.Property(e => e.Description)
                .HasMaxLength(30)
                .HasColumnName("description");
            entity.Property(e => e.Endhour)
                .HasColumnType("smallint(6)")
                .HasColumnName("endhour");
            entity.Property(e => e.Endmin)
                .HasColumnType("smallint(6)")
                .HasColumnName("endmin");
            entity.Property(e => e.Starthour)
                .HasColumnType("smallint(6)")
                .HasColumnName("starthour");
            entity.Property(e => e.Startmin)
                .HasColumnType("smallint(6)")
                .HasColumnName("startmin");
        });

        modelBuilder.Entity<Tperiodtmember>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Carnum })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("tperiodtmember");

            entity.HasIndex(e => e.Regdate, "xrecorddate");

            entity.HasIndex(e => e.Startdate, "xstartdate");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Cardid)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("cardid");
            entity.Property(e => e.Enddate)
                .HasDefaultValueSql("'2013-01-01 00:00:00'")
                .HasColumnType("datetime")
                .HasColumnName("enddate");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Id)
                .HasMaxLength(30)
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");
            entity.Property(e => e.Pindex)
                .HasColumnType("int(11)")
                .HasColumnName("pindex");
            entity.Property(e => e.Regdate)
                .HasDefaultValueSql("'2013-01-01 00:00:00'")
                .HasColumnType("datetime")
                .HasColumnName("regdate");
            entity.Property(e => e.Saletype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("saletype");
            entity.Property(e => e.Saleval)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("saleval");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Startdate)
                .HasDefaultValueSql("'2013-01-01 00:00:00'")
                .HasColumnType("datetime")
                .HasColumnName("startdate");
            entity.Property(e => e.Telnum)
                .HasMaxLength(20)
                .HasColumnName("telnum");
            entity.Property(e => e.Ticketdata)
                .HasMaxLength(45)
                .HasColumnName("ticketdata");
            entity.Property(e => e.Ticketnum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("ticketnum");
            entity.Property(e => e.Visitobject)
                .HasMaxLength(30)
                .HasColumnName("visitobject");
            entity.Property(e => e.Visitplace)
                .HasMaxLength(30)
                .HasColumnName("visitplace");
        });

        modelBuilder.Entity<Tping>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tping");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Xkicccredit)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("xkicccredit");
            entity.Property(e => e.Xparkin)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("xparkin");
            entity.Property(e => e.Xparkinfo)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("xparkinfo");
            entity.Property(e => e.Xperiodin)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("xperiodin");
            entity.Property(e => e.Xperiodinout)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("xperiodinout");
            entity.Property(e => e.Xtmoney)
                .HasColumnType("int(11)")
                .HasColumnName("xtmoney");
        });

        modelBuilder.Entity<Ttcardinfo>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Dealdate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("ttcardinfo");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Dealdate).HasColumnName("dealdate");
            entity.Property(e => e.Acceptnum)
                .HasMaxLength(14)
                .HasColumnName("acceptnum");
            entity.Property(e => e.Accepttype)
                .HasColumnType("smallint(6)")
                .HasColumnName("accepttype");
            entity.Property(e => e.Branchnum)
                .HasMaxLength(20)
                .HasColumnName("branchnum");
            entity.Property(e => e.Cardid)
                .HasMaxLength(30)
                .HasColumnName("cardid");
            entity.Property(e => e.Cardname)
                .HasMaxLength(30)
                .HasColumnName("cardname");
            entity.Property(e => e.Dealnum)
                .HasMaxLength(24)
                .HasColumnName("dealnum");
            entity.Property(e => e.Dealtime)
                .HasMaxLength(10)
                .HasColumnName("dealtime");
            entity.Property(e => e.Dendflag)
                .HasColumnType("smallint(6)")
                .HasColumnName("dendflag");
            entity.Property(e => e.Enddate).HasColumnName("enddate");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Money)
                .HasColumnType("int(11)")
                .HasColumnName("money");
            entity.Property(e => e.Outdevicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("outdevicenum");
            entity.Property(e => e.Parktime)
                .HasColumnType("int(11)")
                .HasColumnName("parktime");
            entity.Property(e => e.Paytype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("paytype");
            entity.Property(e => e.Posid)
                .HasMaxLength(16)
                .HasColumnName("posid");
            entity.Property(e => e.Receiptnum)
                .HasMaxLength(8)
                .HasColumnName("receiptnum");
            entity.Property(e => e.Rescode)
                .HasMaxLength(6)
                .HasColumnName("rescode");
            entity.Property(e => e.Samdealnum)
                .HasMaxLength(20)
                .HasColumnName("samdealnum");
            entity.Property(e => e.Samid)
                .HasMaxLength(20)
                .HasColumnName("samid");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Tendflag)
                .HasColumnType("smallint(6)")
                .HasColumnName("tendflag");
            entity.Property(e => e.Termid)
                .HasMaxLength(16)
                .HasColumnName("termid");
            entity.Property(e => e.Ticketdata)
                .HasMaxLength(20)
                .HasColumnName("ticketdata");
        });

        modelBuilder.Entity<Tvaninfo>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("tvaninfo");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Branchcode)
                .HasMaxLength(12)
                .HasDefaultValueSql("''")
                .HasColumnName("branchcode");
            entity.Property(e => e.Branchname)
                .HasMaxLength(45)
                .HasDefaultValueSql("''")
                .HasColumnName("branchname");
            entity.Property(e => e.Vancode)
                .HasColumnType("int(11)")
                .HasColumnName("vancode");
        });

        modelBuilder.Entity<Txblacklist>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Carnum })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("txblacklist");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Enddate).HasColumnName("enddate");
            entity.Property(e => e.Msg)
                .HasMaxLength(200)
                .HasColumnName("msg");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
            entity.Property(e => e.Regdate).HasColumnName("regdate");
            entity.Property(e => e.Startdate).HasColumnName("startdate");
            entity.Property(e => e.Telnum)
                .HasMaxLength(30)
                .HasColumnName("telnum");
        });

        modelBuilder.Entity<Txpark>(entity =>
        {
            entity.HasKey(e => new { e.Xindex, e.Outdate })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("txpark");

            entity.HasIndex(e => e.Carnum, "xcarnum");

            entity.HasIndex(e => e.Ticketdata, "xticketdata");

            entity.HasIndex(e => e.Ticketnum, "xticketnum");

            entity.Property(e => e.Xindex)
                .ValueGeneratedOnAdd()
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Outdate)
                .HasDefaultValueSql("'2022-01-01 12:34:56'")
                .HasColumnType("datetime")
                .HasColumnName("outdate");
            entity.Property(e => e.Carnum)
                .HasMaxLength(20)
                .HasColumnName("carnum");
            entity.Property(e => e.Groupnum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Indate)
                .HasColumnType("datetime")
                .HasColumnName("indate");
            entity.Property(e => e.Indevicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("indevicenum");
            entity.Property(e => e.Inimage)
                .HasMaxLength(80)
                .HasColumnName("inimage");
            entity.Property(e => e.Intick)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("intick");
            entity.Property(e => e.Managercode)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("managercode");
            entity.Property(e => e.Managername)
                .HasMaxLength(30)
                .HasColumnName("managername");
            entity.Property(e => e.Outdevicenum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("outdevicenum");
            entity.Property(e => e.Outimage)
                .HasMaxLength(80)
                .HasColumnName("outimage");
            entity.Property(e => e.Outtick)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("outtick");
            entity.Property(e => e.Parkcaltype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("parkcaltype");
            entity.Property(e => e.Parkcartype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("parkcartype");
            entity.Property(e => e.Parkmoney)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("parkmoney");
            entity.Property(e => e.Parktime)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("parktime");
            entity.Property(e => e.Pindex)
                .HasColumnType("int(11)")
                .HasColumnName("pindex");
            entity.Property(e => e.Sitenum)
                .HasDefaultValueSql("'1'")
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Ticketcartype)
                .HasDefaultValueSql("'0'")
                .HasColumnType("smallint(6)")
                .HasColumnName("ticketcartype");
            entity.Property(e => e.Ticketdata)
                .HasMaxLength(30)
                .HasColumnName("ticketdata");
            entity.Property(e => e.Ticketnum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("ticketnum");
        });

        modelBuilder.Entity<Txparknum>(entity =>
        {
            entity.HasKey(e => e.Xindex).HasName("PRIMARY");

            entity.ToTable("txparknum");

            entity.Property(e => e.Xindex)
                .HasColumnType("int(11)")
                .HasColumnName("xindex");
            entity.Property(e => e.Devicenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicenum");
            entity.Property(e => e.Devicetype)
                .HasColumnType("smallint(6)")
                .HasColumnName("devicetype");
            entity.Property(e => e.Groupnum)
                .HasColumnType("smallint(6)")
                .HasColumnName("groupnum");
            entity.Property(e => e.Iparkfullnum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("iparkfullnum");
            entity.Property(e => e.Jparkfullnum)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("jparkfullnum");
            entity.Property(e => e.Parkfullnum)
                .HasDefaultValueSql("'0000000000'")
                .HasColumnType("int(10) unsigned zerofill")
                .HasColumnName("parkfullnum");
            entity.Property(e => e.Parkinnum)
                .HasDefaultValueSql("'0000000000'")
                .HasColumnType("int(10) unsigned zerofill")
                .HasColumnName("parkinnum");
            entity.Property(e => e.Parkoutnum)
                .HasDefaultValueSql("'0000000000'")
                .HasColumnType("int(10) unsigned zerofill")
                .HasColumnName("parkoutnum");
            entity.Property(e => e.Sitenum)
                .HasColumnType("smallint(6)")
                .HasColumnName("sitenum");
            entity.Property(e => e.Terminnum)
                .HasDefaultValueSql("'0000000000'")
                .HasColumnType("int(10) unsigned zerofill")
                .HasColumnName("terminnum");
            entity.Property(e => e.Termoutnum)
                .HasDefaultValueSql("'0000000000'")
                .HasColumnType("int(10) unsigned zerofill")
                .HasColumnName("termoutnum");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
