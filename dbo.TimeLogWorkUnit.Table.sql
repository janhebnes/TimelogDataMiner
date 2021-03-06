USE [DataWarehouse]
GO
/****** Object:  Table [dbo].[TimeLogWorkUnit]    Script Date: 09-12-2015 16:52:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TimeLogWorkUnit](
	[ID] [int] NOT NULL,
	[EmployeeID] [int] NULL,
	[EmployeeInitials] [nvarchar](100) NULL,
	[EmployeeFirstName] [nvarchar](100) NULL,
	[EmployeeLastName] [nvarchar](100) NULL,
	[AllocationID] [int] NULL,
	[TaskID] [int] NULL,
	[ProjectID] [int] NULL,
	[Date] [date] NULL,
	[Note] [nvarchar](max) NULL,
	[AdditionalTextField] [nvarchar](800) NULL,
	[RegHours] [decimal](18, 2) NULL,
	[Billable] [decimal](18, 2) NULL,
	[InvHours] [decimal](18, 2) NULL,
	[CostAmount] [decimal](18, 2) NULL,
	[RegAmount] [decimal](18, 2) NULL,
	[InvAmount] [decimal](18, 2) NULL,
 CONSTRAINT [PK_WorkUnit] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
