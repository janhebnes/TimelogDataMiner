USE [DataWarehouse]
GO
/****** Object:  Table [dbo].[TimeLogSupportJournal]    Script Date: 09-12-2015 16:52:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TimeLogSupportJournal](
	[ID] [int] NOT NULL,
	[Date] [datetime] NULL,
	[StartTime] [time](7) NULL,
	[EndTime] [time](7) NULL,
	[RegMinutes] [int] NULL,
	[Comment] [nvarchar](max) NULL,
	[RegHours] [decimal](18, 2) NULL,
	[InvHours] [decimal](18, 2) NULL,
	[CostAmount] [decimal](18, 2) NULL,
	[RegAmount] [decimal](18, 2) NULL,
	[InvAmount] [decimal](18, 2) NULL,
	[EmployeeID] [int] NULL,
	[EmployeeInitials] [nvarchar](50) NULL,
	[EmployeeFullName] [nvarchar](150) NULL,
	[CustomerID] [int] NULL,
	[CustomerName] [nvarchar](150) NULL,
	[CustomerNo] [nvarchar](50) NULL,
	[SupportCaseID] [int] NULL,
	[SupportCaseHeader] [nvarchar](150) NULL,
	[SupportCaseNo] [nvarchar](50) NULL,
	[SupportContractID] [int] NULL,
	[SupportContractName] [nvarchar](150) NULL,
	[SupportContractNo] [nvarchar](50) NULL,
 CONSTRAINT [PK_SupportJournal] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
