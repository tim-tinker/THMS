using System.ComponentModel;
using System.Globalization;
using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class StatementEditorDialog : Form
    {
        private static readonly Font TitleFont = new("Segoe UI", 14F, FontStyle.Bold);
        private static readonly Font IdentityFont = new("Segoe UI", 10F, FontStyle.Bold);
        private static readonly Font SectionFont = new("Segoe UI", 11F, FontStyle.Bold);
        private static readonly Color SectionBarColor = Color.FromArgb(255, 232, 168);

        private readonly IAccountStatementDataStore _statementStore;
        private readonly PlanningOrchestrator? _orchestrator;
        private readonly AccountStatement? _existingStatement;
        private readonly Account? _lockedAccount;
        private readonly List<Account> _accounts;
        private bool _loading;
        private bool _suppressEditorEvents;
        private Guid? _datedForAccountId;
        private StatementType? _composedType;
        private DateTime? _chosenStatementDate;
        private DateTime? _chosenDueDate;
        private decimal? _chosenAmountDue;
        private decimal? _creditLimit;

        private DateTimePicker dtStatementDate = null!;
        private DateTimePicker dtDueDate = null!;
        private NumericUpDown numAmountDue = null!;
        private TextBox txtNotes = null!;
        private Label? lblPeriod;
        private Label? lblPreviousBalance;
        private Label? lblAvailableCredit;

        private NumericUpDown? numEscrowBalance;
        private NumericUpDown? numStatementBalance;
        private DataGridView? gridPromotions;
        private DataGridView? gridUsage;
        private DataGridView? gridCharges;
        private BindingList<PromotionEditRow> _promotions = [];
        private BindingList<UsageEditRow> _usage = [];
        private BindingList<ChargeEditRow> _charges = [];

        public StatementEditorDialog(
            PlanningOrchestrator orchestrator,
            AccountStatement? existingStatement)
            : this(orchestrator, existingStatement, lockedAccount: null)
        {
        }

        public StatementEditorDialog(
            PlanningOrchestrator orchestrator,
            AccountStatement? existingStatement,
            Account? lockedAccount)
            : this(
                orchestrator.GetStatementStore(),
                existingStatement,
                orchestrator.GetAccounts(),
                lockedAccount)
        {
            _orchestrator = orchestrator;
        }

        public StatementEditorDialog(
            IAccountStatementDataStore statementStore,
            AccountStatement? existingStatement)
            : this(
                statementStore,
                existingStatement,
                new DataStoreFactory().GetAccountStore().GetAllAccounts().ToList())
        {
        }

        public StatementEditorDialog(
            IAccountStatementDataStore statementStore,
            AccountStatement? existingStatement,
            IReadOnlyList<Account> accounts,
            Account? lockedAccount = null)
        {
            _statementStore = statementStore;
            _existingStatement = existingStatement;
            _lockedAccount = lockedAccount;
            _accounts = accounts.ToList();
            if (_lockedAccount is not null && _accounts.All(a => a.Id != _lockedAccount.Id))
                _accounts.Add(_lockedAccount);

            InitializeComponent();
            CreateEditors();
            cboAccount.SelectedIndexChanged += OnAccountChanged;
            WireComboDrawing();
            _loading = true;
            LoadExisting();
            ApplyLockedAccountUi();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            SizeDialogInputs();
            ComposeStatement(SelectedType());
            FitToWorkingArea();
            BeginInvoke(FitPromotionDateColumns);
        }

        private void WireComboDrawing()
        {
            cboAccount.DrawMode = DrawMode.OwnerDrawFixed;
            cboAccount.IntegralHeight = false;
            cboAccount.DrawItem -= OnComboDrawItem;
            cboAccount.DrawItem += OnComboDrawItem;
        }

        private void SizeDialogInputs()
        {
            var comboHeight = Math.Max(Scale(32), Font.Height + Scale(14));
            cboAccount.ItemHeight = Math.Max(Scale(20), Font.Height);
            cboAccount.Height = comboHeight;

            var showAccount = _lockedAccount is null;
            accountBar.Visible = showAccount;
            layout.RowStyles[0].SizeType = SizeType.Absolute;
            layout.RowStyles[0].Height = showAccount ? comboHeight + Scale(8) : 0;
            pnlDocument.Padding = new Padding(Scale(20), Scale(16), Scale(16), Scale(12));
            pnlDocument.BackColor = Color.White;
        }

        private static void OnComboDrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ComboBox combo)
                return;

            e.DrawBackground();
            if (e.Index >= 0 && e.Index < combo.Items.Count)
            {
                var selected = (e.State & DrawItemState.Selected) != 0;
                var color = selected ? SystemColors.HighlightText : combo.ForeColor;
                TextRenderer.DrawText(
                    e.Graphics,
                    combo.GetItemText(combo.Items[e.Index]),
                    combo.Font,
                    e.Bounds,
                    color,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            e.DrawFocusRectangle();
        }

        private void FitToWorkingArea()
        {
            var area = Screen.FromControl(this).WorkingArea;
            var margin = LogicalToDeviceUnits(48);
            var maxWidth = Math.Max(LogicalToDeviceUnits(560), area.Width - margin);
            var maxHeight = Math.Max(LogicalToDeviceUnits(420), area.Height - margin);

            MinimumSize = new Size(
                Math.Min(MinimumSize.Width, maxWidth),
                Math.Min(MinimumSize.Height, maxHeight));
            Size = new Size(
                Math.Min(Width, maxWidth),
                Math.Min(Height, maxHeight));
            Location = new Point(
                area.Left + Math.Max(0, (area.Width - Width) / 2),
                area.Top + Math.Max(0, (area.Height - Height) / 2));
        }

        private void BindAccounts(Guid? selectedId = null)
        {
            var items = _accounts
                .Where(a => StatementAccountMatch.ForAccount(a) is not null || a.Id == selectedId)
                .OrderBy(a => a.Name)
                .Select(a => new AccountListItem(a))
                .ToList();

            cboAccount.DisplayMember = nameof(AccountListItem.Label);
            cboAccount.ValueMember = nameof(AccountListItem.Id);
            cboAccount.DataSource = items;

            if (selectedId is Guid id)
            {
                var match = items.FirstOrDefault(i => i.Id == id);
                cboAccount.SelectedItem = match;
                if (match is null)
                    cboAccount.SelectedIndex = -1;
            }
            else
            {
                cboAccount.SelectedIndex = -1;
            }
        }

        private void ApplyLockedAccountUi()
        {
            if (_lockedAccount is null)
                return;

            btnNewAccount.Visible = false;
            cboAccount.Enabled = false;
            accountBar.Visible = false;
            layout.RowStyles[0].Height = 0;
            if (IsHandleCreated)
                SizeDialogInputs();
        }

        private void LoadExisting()
        {
            _loading = true;
            numAmountDue.Value = 0;

            if (_existingStatement is null)
            {
                if (_lockedAccount is Account locked
                    && StatementAccountMatch.ForAccount(locked) is StatementType lockedType)
                {
                    BindAccounts(locked.Id);
                    _loading = false;
                    ApplyNewStatementDates();
                    LoadTypePanel(lockedType);
                    CopyPreviousPromotions();
                    return;
                }

                BindAccounts();
                _loading = false;
                ApplyNewStatementDates();
                ComposeStatement(null);
                return;
            }

            BindAccounts(_existingStatement.AccountId);
            dtStatementDate.Value = SafeDate(_existingStatement.StatementDate);
            dtDueDate.Value = SafeDate(_existingStatement.DueDate);
            txtNotes.Text = _existingStatement.Notes ?? "";
            if (!StatementAccountMatch.IsNonPayable(_existingStatement))
                SetMoney(numAmountDue, _existingStatement.AmountDue);

            _loading = false;
            LoadTypePanel(_existingStatement.Type);
            BindTypeSpecific(_existingStatement);
        }

        private void OnAccountChanged(object? sender, EventArgs e)
        {
            if (_loading || _existingStatement is not null)
                return;

            // Binding the account list raises this again when the combo creates
            // its handle. Rebuilding then throws away dates, the amount, and
            // line items the user already typed.
            var account = SelectedAccount() ?? _lockedAccount;
            var type = account is null ? null : StatementAccountMatch.ForAccount(account);
            if (account is not null && _datedForAccountId == account.Id && _composedType == type)
                return;

            ApplyNewStatementDates();
            if (type is not StatementType selected)
            {
                ComposeStatement(null);
                return;
            }

            LoadTypePanel(selected);
            CopyPreviousPromotions();
        }

        private void ApplyNewStatementDates()
        {
            if (_existingStatement is not null)
                return;

            var account = SelectedAccount() ?? _lockedAccount;
            if (account is not null && _datedForAccountId == account.Id)
                return;

            var existing = account is null ? [] : _statementStore.GetForAccount(account.Id);
            var dates = NextStatementDates.Suggest(existing, DateTime.Today);
            _suppressEditorEvents = true;
            try
            {
                dtStatementDate.Value = SafeDate(dates.StatementDate);
                dtDueDate.Value = SafeDate(dates.DueDate);
            }
            finally
            {
                _suppressEditorEvents = false;
            }

            _datedForAccountId = account?.Id;
        }

        private void OnNewAccount(object? sender, EventArgs e)
        {
            var seed = SelectedType() is StatementType type
                ? StatementAccountMatch.CreateAccount(type)
                : new UntrackedAccount { Type = AccountType.Utility };

            using var dlg = new AccountEditForm(seed, _accounts);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                new AccountOrchestrator().Save(dlg.Account);
                _orchestrator?.SyncAutoPayForAccount(dlg.Account);
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(this, "Account", ex);
                return;
            }

            _accounts.RemoveAll(a => a.Id == dlg.Account.Id);
            _accounts.Add(dlg.Account);

            _loading = true;
            BindAccounts(dlg.Account.Id);
            _loading = false;
            if (SelectedType() is StatementType selected)
            {
                LoadTypePanel(selected);
                CopyPreviousPromotions();
            }
            else
            {
                ComposeStatement(null);
            }
        }

        private StatementType? SelectedType()
        {
            if (_existingStatement is not null)
                return _existingStatement.Type;

            var account = SelectedAccount() ?? _lockedAccount;
            return account is null ? null : StatementAccountMatch.ForAccount(account);
        }

        private Account? SelectedAccount() =>
            cboAccount.SelectedItem is AccountListItem item ? item.Account : null;

        private void OnStatementAccountChanged(object? sender, EventArgs e)
        {
            if (_loading)
                return;
            CopyPreviousPromotions();
        }

        private void LoadTypePanel(StatementType type)
        {
            _promotions = [];
            _usage = [];
            _charges = [];
            SetMoney(numStatementBalance, 0);
            SetMoney(numEscrowBalance, 0);
            ComposeStatement(type);
        }

        private void CreateEditors()
        {
            dtStatementDate = new DateTimePicker { Format = DateTimePickerFormat.Short };
            dtDueDate = new DateTimePicker { Format = DateTimePickerFormat.Short };
            numAmountDue = MoneyBox();
            numStatementBalance = MoneyBox();
            numEscrowBalance = MoneyBox();
            numStatementBalance.Font = new Font(Font, FontStyle.Bold);
            txtNotes = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle
            };
            dtStatementDate.ValueChanged += OnStatementDateChanged;
            dtDueDate.ValueChanged += OnDueDateChanged;
            numAmountDue.ValueChanged += OnAmountDueChanged;
            numStatementBalance.ValueChanged += (_, _) => RefreshAvailableCredit();
        }

        private void OnStatementDateChanged(object? sender, EventArgs e)
        {
            if (_loading)
                return;
            if (!_suppressEditorEvents)
                _chosenStatementDate = dtStatementDate.Value.Date;
            if (_suppressEditorEvents)
                return;
            RefreshPeriod();
            RefreshPreviousBalance();
        }

        private void OnDueDateChanged(object? sender, EventArgs e)
        {
            if (_loading || _suppressEditorEvents)
                return;
            _chosenDueDate = dtDueDate.Value.Date;
        }

        private void OnAmountDueChanged(object? sender, EventArgs e)
        {
            if (_loading || _suppressEditorEvents)
                return;
            _chosenAmountDue = numAmountDue.Value;
        }

        private void ComposeStatement(StatementType? type)
        {
            _suppressEditorEvents = true;
            try
            {
                ComposeStatementPage(type);
                RestoreChosenEditors();
            }
            finally
            {
                _suppressEditorEvents = false;
            }
        }

        private void RestoreChosenEditors()
        {
            if (_chosenStatementDate is DateTime statementDate)
                dtStatementDate.Value = SafeDate(statementDate);
            if (_chosenDueDate is DateTime dueDate)
                dtDueDate.Value = SafeDate(dueDate);
            if (_chosenAmountDue is decimal amount)
                SetMoney(numAmountDue, amount);
        }

        private void ComposeStatementPage(StatementType? type)
        {
            _composedType = type;
            lblPeriod = null;
            lblPreviousBalance = null;
            lblAvailableCredit = null;
            _creditLimit = null;
            gridPromotions = null;
            gridUsage = null;
            gridCharges = null;
            DetachEditors();

            pnlDocument.SuspendLayout();
            pnlDocument.AutoScroll = false;
            var stale = pnlDocument.Controls.Cast<Control>().ToList();
            pnlDocument.Controls.Clear();
            foreach (var child in stale)
                child.Dispose();

            var host = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderHeight()));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var header = BuildHeader();
            header.Dock = DockStyle.Fill;
            header.Margin = Padding.Empty;
            host.Controls.Add(header, 0, 0);

            var scroller = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Margin = Padding.Empty
            };

            var page = new TableLayoutPanel
            {
                ColumnCount = 1,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var row = 0;
            var total = 0;
            void Add(Control control, int height)
            {
                page.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
                control.Dock = DockStyle.Fill;
                control.Margin = Padding.Empty;
                page.Controls.Add(control, 0, row);
                row++;
                total += height;
            }

            if (BuildFacts(type) is (Control facts, int factsHeight))
                Add(facts, factsHeight);

            if (type is StatementType lineItemType && BuildLineItems(lineItemType) is Control lineItems)
                Add(lineItems, LineItemHeight(lineItemType));

            Add(BuildNotes(), NotesHeight());

            page.RowCount = row;
            page.MinimumSize = new Size(0, total);
            page.Height = total;
            page.Dock = DockStyle.Top;
            scroller.Controls.Add(page);
            host.Controls.Add(scroller, 0, 1);
            pnlDocument.Controls.Add(host);
            pnlDocument.ResumeLayout();
        }

        private void DetachEditors()
        {
            Detach(dtStatementDate);
            Detach(dtDueDate);
            Detach(numAmountDue);
            Detach(numStatementBalance);
            Detach(numEscrowBalance);
            Detach(txtNotes);
        }

        private static void Detach(Control? control) =>
            control?.Parent?.Controls.Remove(control);

        private Control BuildHeader()
        {
            var account = SelectedAccount() ?? _lockedAccount;
            var ending = EndingIn(account?.AccountNumber);
            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                RowCount = ending is null ? 2 : 3
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var name = new Label
            {
                Text = string.IsNullOrWhiteSpace(account?.Name) ? "Select an account" : account!.Name,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true,
                Font = account is null ? Font : TitleFont,
                ForeColor = account is null ? SystemColors.GrayText : Color.Black,
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, Scale(4), 0)
            };
            stack.Controls.Add(name, 0, 0);
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, NameRowHeight));

            var row = 1;
            if (ending is not null)
            {
                stack.Controls.Add(new Label
                {
                    Text = ending,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoEllipsis = true,
                    Font = IdentityFont,
                    Margin = Padding.Empty,
                    Padding = new Padding(0, 0, Scale(4), 0)
                }, 0, row);
                stack.RowStyles.Add(new RowStyle(SizeType.Absolute, EndingRowHeight));
                row++;
            }

            lblPeriod = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true,
                Font = IdentityFont,
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, Scale(8), 0)
            };
            RefreshPeriod();

            var period = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            period.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            period.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ValueColumnWidth()));
            period.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            period.Controls.Add(lblPeriod, 0, 0);
            period.Controls.Add(EditorHost(dtStatementDate, RowHeight), 1, 0);
            stack.Controls.Add(period, 0, row);
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));
            return stack;
        }

        private int HeaderHeight()
        {
            var ending = EndingIn((SelectedAccount() ?? _lockedAccount)?.AccountNumber);
            return NameRowHeight + (ending is null ? 0 : EndingRowHeight) + RowHeight;
        }

        private void RefreshPeriod()
        {
            if (lblPeriod is null)
                return;

            var previous = CurrentPreviousStatement();
            lblPeriod.Text = previous is null
                ? "Statement date"
                : $"Statement Period {previous.StatementDate.Date.AddDays(1):d} to";
        }

        private (Control Control, int Height)? BuildFacts(StatementType? type)
        {
            Control? payment = null;
            var paymentHeight = 0;
            if (type is StatementType payable && !StatementAccountMatch.IsNonPayable(payable))
            {
                payment = BuildPaymentSection(payable);
                paymentHeight = SectionHeight(2);
            }

            Control? summary = null;
            var summaryHeight = 0;
            if (type is StatementType summaryType && SummaryRows(summaryType) is { Count: > 0 } rows)
            {
                summary = Section("Account Summary", rows);
                summaryHeight = SectionHeight(rows.Count);
            }

            if (payment is null && summary is null)
                return null;
            if (payment is null)
                return (summary!, summaryHeight);
            if (summary is null)
                return (payment, paymentHeight);

            var pair = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pair.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            payment.Dock = DockStyle.Top;
            payment.Height = paymentHeight;
            payment.Margin = new Padding(0, 0, Scale(8), 0);
            summary.Margin = new Padding(Scale(8), 0, 0, 0);
            pair.Controls.Add(payment, 0, 0);
            pair.Controls.Add(summary, 1, 0);
            return (pair, Math.Max(paymentHeight, summaryHeight));
        }

        private Control BuildPaymentSection(StatementType type)
        {
            var amountLabel = type == StatementType.CreditCard ? "Minimum Payment" : "Amount Due";
            return Section("Payment",
            [
                StatementRow("Payment Due Date", dtDueDate),
                StatementRow(amountLabel, numAmountDue)
            ]);
        }

        private List<Control> SummaryRows(StatementType type)
        {
            if (type is StatementType.Utility or StatementType.Service or StatementType.Insurance)
                return [];

            var rows = new List<Control>();
            lblPreviousBalance = ValueLabel("—");
            RefreshPreviousBalance();
            rows.Add(StatementRow("Previous Balance", lblPreviousBalance));

            switch (type)
            {
                case StatementType.CreditCard:
                    rows.Add(StatementRow("New Balance", numStatementBalance!, bold: true));
                    if ((SelectedAccount() ?? _lockedAccount) is CreditAccount credit)
                    {
                        _creditLimit = credit.CreditLimit;
                        rows.Add(StatementRow("Credit Limit", ValueLabel(credit.CreditLimit.ToString("C2"))));
                        lblAvailableCredit = ValueLabel((credit.CreditLimit - numStatementBalance!.Value).ToString("C2"));
                        rows.Add(StatementRow("Available Credit", lblAvailableCredit));
                        rows.Add(StatementRow("APR", ValueLabel(Percent(credit.APR))));
                    }
                    break;
                case StatementType.Bank or StatementType.Investment:
                    rows.Add(StatementRow("New Balance", numStatementBalance!, bold: true));
                    break;
                case StatementType.Loan:
                    rows.Add(StatementRow("New Balance", numStatementBalance!, bold: true));
                    if ((SelectedAccount() ?? _lockedAccount) is LoanAccount loan)
                        rows.Add(StatementRow("Interest Rate", ValueLabel(Percent(loan.InterestRate))));
                    break;
                case StatementType.Mortgage:
                    rows.Add(StatementRow("Outstanding Principal", numStatementBalance!, bold: true));
                    rows.Add(StatementRow("Escrow Balance", numEscrowBalance!));
                    if ((SelectedAccount() ?? _lockedAccount) is MortgageAccount mortgage)
                        rows.Add(StatementRow("Interest Rate", ValueLabel(Percent(mortgage.InterestRate))));
                    break;
            }

            return rows;
        }

        private void RefreshPreviousBalance()
        {
            if (lblPreviousBalance is null)
                return;
            var balance = CurrentPreviousStatement() is AccountStatement previous
                ? BalanceOf(previous)
                : null;
            lblPreviousBalance.Text = balance is decimal amount ? amount.ToString("C2") : "—";
        }

        private void RefreshAvailableCredit()
        {
            if (lblAvailableCredit is null || _creditLimit is not decimal limit || numStatementBalance is null)
                return;
            lblAvailableCredit.Text = (limit - numStatementBalance.Value).ToString("C2");
        }

        private AccountStatement? CurrentPreviousStatement()
        {
            var account = SelectedAccount() ?? _lockedAccount;
            if (account is null)
                return null;

            var end = dtStatementDate.Value.Date;
            return _statementStore.GetForAccount(account.Id)
                .Where(statement => statement.Id != _existingStatement?.Id && statement.StatementDate.Date < end)
                .OrderByDescending(statement => statement.StatementDate.Date)
                .ThenByDescending(statement => statement.DueDate.Date)
                .FirstOrDefault();
        }

        private static decimal? BalanceOf(AccountStatement statement) => statement switch
        {
            BankStatement bank => bank.StatementBalance,
            InvestmentStatement investment => investment.StatementBalance,
            LoanStatement loan => loan.StatementBalance,
            MortgageStatement mortgage => mortgage.StatementBalance,
            CreditCardStatement card => card.StatementBalance,
            _ => null
        };

        private static string? EndingIn(string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return null;
            var digits = new string(accountNumber.Where(char.IsDigit).ToArray());
            if (digits.Length == 0)
                return null;
            var last = digits.Length <= 4 ? digits : digits[^4..];
            return $"Account ending in {last}";
        }

        private static string Percent(decimal rate) => rate.ToString("0.00") + "%";

        private Control? BuildLineItems(StatementType type) => type switch
        {
            StatementType.CreditCard => BuildCreditCardPanel(),
            StatementType.Utility => BuildUtilityPanel(),
            StatementType.Service => BuildServicePanel(),
            _ => null
        };

        private static int LineItemLogicalHeight(StatementType type) => type switch
        {
            StatementType.Utility => 360,
            StatementType.CreditCard or StatementType.Service => 260,
            _ => 0
        };

        private int LineItemHeight(StatementType type) => Scale(LineItemLogicalHeight(type));

        private Control BuildCreditCardPanel()
        {
            var panel = LineItemHost(3);
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, BarHeight));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var promotions = CreateGrid();
            gridPromotions = promotions;
            promotions.Columns.Add(DatePickerColumn(nameof(PromotionEditRow.DateAcquired), "Date Acquired"));
            promotions.Columns.Add(AmountColumn(nameof(PromotionEditRow.InitialAmount), "Initial Amount"));
            promotions.Columns.Add(AmountColumn(nameof(PromotionEditRow.CurrentBalance), "Current Balance"));
            promotions.Columns.Add(DatePickerColumn(nameof(PromotionEditRow.Deadline), "Deadline"));
            promotions.Columns.Add(PromoTypeColumn());
            promotions.HandleCreated += (_, _) => QueueFitPromotionDateColumns();
            promotions.DataBindingComplete += (_, _) => QueueFitPromotionDateColumns();
            promotions.RowsAdded += (_, _) => QueueFitPromotionDateColumns();
            promotions.DeleteRow += (_, _) => DeleteSelected(_promotions, promotions);
            promotions.DataSource = _promotions;
            AttachPromotionMenu(promotions);

            panel.Controls.Add(SectionBar("Promotions"), 0, 0);
            panel.Controls.Add(gridPromotions, 0, 1);
            panel.Controls.Add(AddBar(OnAddPromotion), 0, 2);
            return panel;
        }

        private Control BuildUtilityPanel()
        {
            var panel = LineItemHost(6);
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, BarHeight));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, BarHeight));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var usage = CreateGrid();
            usage.AppendRow += (_, _) => AppendUsageRow();
            gridUsage = usage;
            gridUsage.Columns.Add(TextColumn(nameof(UsageEditRow.Description), "Description"));
            gridUsage.Columns.Add(AmountColumn(nameof(UsageEditRow.Amount), "Amount"));
            gridUsage.Columns.Add(ComboColumn(
                nameof(UsageEditRow.Type),
                "Type",
                ["kWh", "gallons", "GB", "minutes", "CCF"]));
            gridUsage.Columns.Add(RateColumn(nameof(UsageEditRow.Rate), "Rate"));
            gridUsage.Columns.Add(CostColumn());
            gridUsage.DataSource = _usage;

            var charges = CreateGrid();
            charges.AppendRow += (_, _) => AppendChargeRow();
            gridCharges = charges;
            gridCharges.Columns.Add(TextColumn(nameof(ChargeEditRow.Description), "Description"));
            gridCharges.Columns.Add(AmountColumn(nameof(ChargeEditRow.Amount), "Amount"));
            gridCharges.DataSource = _charges;

            panel.Controls.Add(SectionBar("Usage"), 0, 0);
            panel.Controls.Add(GridToolbar("Add Usage", "Delete Usage", OnAddUsage, OnDeleteUsage), 0, 1);
            panel.Controls.Add(gridUsage, 0, 2);
            panel.Controls.Add(SectionBar("Charges"), 0, 3);
            panel.Controls.Add(GridToolbar("Add Charge", "Delete Charge", OnAddCharge, OnDeleteCharge), 0, 4);
            panel.Controls.Add(gridCharges, 0, 5);
            return panel;
        }

        private Control BuildServicePanel()
        {
            var panel = LineItemHost(3);
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, BarHeight));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var charges = CreateGrid();
            charges.AppendRow += (_, _) => AppendChargeRow();
            gridCharges = charges;
            gridCharges.Columns.Add(TextColumn(nameof(ChargeEditRow.Description), "Description"));
            gridCharges.Columns.Add(AmountColumn(nameof(ChargeEditRow.Amount), "Amount"));
            gridCharges.DataSource = _charges;

            panel.Controls.Add(SectionBar("Charges"), 0, 0);
            panel.Controls.Add(GridToolbar("Add Charge", "Delete Charge", OnAddCharge, OnDeleteCharge), 0, 1);
            panel.Controls.Add(gridCharges, 0, 2);
            return panel;
        }

        private TableLayoutPanel LineItemHost(int rows)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = rows,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = new Padding(0, Scale(8), 0, 0)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return panel;
        }

        private Control BuildNotes()
        {
            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            stack.Controls.Add(Spacer(Scale(8)), 0, 0);
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(8)));
            stack.Controls.Add(SectionBar("Notes"), 0, 1);
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, BarHeight));
            txtNotes.Dock = DockStyle.Fill;
            txtNotes.Margin = new Padding(0, Scale(6), 0, 0);
            stack.Controls.Add(txtNotes, 0, 2);
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return stack;
        }

        private int NotesHeight() => Scale(8) + BarHeight + Scale(64);

        private Control Section(string title, IReadOnlyList<Control> rows)
        {
            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = rows.Count + 2,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            stack.Controls.Add(Spacer(Scale(8)), 0, 0);
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(8)));
            stack.Controls.Add(SectionBar(title), 0, 1);
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, BarHeight));
            for (var i = 0; i < rows.Count; i++)
            {
                stack.Controls.Add(rows[i], 0, i + 2);
                stack.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));
            }

            return stack;
        }

        private int SectionHeight(int dataRows) => Scale(8) + BarHeight + RowHeight * dataRows;

        private Label SectionBar(string title) => new()
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = SectionFont,
            BackColor = SectionBarColor,
            ForeColor = Color.Black,
            Padding = new Padding(Scale(10), 0, Scale(8), 0),
            Margin = Padding.Empty
        };

        private Control StatementRow(string caption, Control value, bool bold = false)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.White
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ValueColumnWidth()));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            row.Controls.Add(new Label
            {
                Text = caption,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = bold ? new Font(Font, FontStyle.Bold) : Font,
                Margin = Padding.Empty,
                Padding = new Padding(Scale(4), 0, Scale(6), 0)
            }, 0, 0);

            if (value is Label readOnly)
            {
                readOnly.Dock = DockStyle.Fill;
                readOnly.TextAlign = ContentAlignment.MiddleRight;
                readOnly.Font = bold ? new Font(Font, FontStyle.Bold) : Font;
                readOnly.Padding = new Padding(0, 0, Scale(2), 0);
                readOnly.Margin = Padding.Empty;
                row.Controls.Add(readOnly, 1, 0);
            }
            else
            {
                row.Controls.Add(EditorHost(value, RowHeight), 1, 0);
            }

            return row;
        }

        private Control EditorHost(Control editor, int rowHeight)
        {
            var height = editor.PreferredSize.Height;
            if (height <= 0)
                height = editor.Height > 0 ? editor.Height : Scale(23);
            var extra = Math.Max(0, rowHeight - height);
            var top = extra / 2;
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = new Padding(0, top, 0, extra - top),
                BackColor = Color.White
            };
            editor.Dock = DockStyle.Fill;
            editor.Margin = Padding.Empty;
            host.Controls.Add(editor);
            return host;
        }

        private Label ValueLabel(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };

        private static Panel Spacer(int height) => new()
        {
            Height = height,
            Margin = Padding.Empty,
            BackColor = Color.White
        };

        private int Scale(int logical) => LogicalToDeviceUnits(logical);

        private int NameRowHeight => Scale(32);

        private int EndingRowHeight => Scale(22);

        private int RowHeight => Math.Max(Scale(26), EditorBandHeight + Scale(4));

        private int EditorBandHeight =>
            Math.Max(dtStatementDate.PreferredSize.Height, numAmountDue.PreferredSize.Height);

        private int BarHeight => Scale(26);

        private int ValueColumnWidth() => Scale(176);

        private void BindTypeSpecific(AccountStatement statement)
        {
            switch (statement)
            {
                case BankStatement bank:
                    SetMoney(numStatementBalance, bank.StatementBalance);
                    break;
                case InvestmentStatement investment:
                    SetMoney(numStatementBalance, investment.StatementBalance);
                    break;
                case LoanStatement loan:
                    SetMoney(numStatementBalance, loan.StatementBalance);
                    break;
                case MortgageStatement mortgage:
                    SetMoney(numStatementBalance, mortgage.StatementBalance);
                    SetMoney(numEscrowBalance, mortgage.EscrowBalance);
                    break;
                case CreditCardStatement card:
                    SetMoney(numStatementBalance, card.StatementBalance);
                    foreach (var promo in card.Promotions)
                    {
                        _promotions.Add(new PromotionEditRow
                        {
                            Id = promo.Id,
                            DateAcquired = promo.DateAcquired.Year > 1
                                ? promo.DateAcquired.Date
                                : SafeDate(card.StatementDate),
                            InitialAmount = promo.InitialAmount > 0 ? promo.InitialAmount : promo.CurrentBalance,
                            CurrentBalance = promo.CurrentBalance,
                            Deadline = SafeDate(promo.Deadline),
                            Type = promo.Type
                        });
                    }
                    break;
                case UtilityStatement utility:
                    foreach (var usage in utility.Usage)
                        _usage.Add(new UsageEditRow
                        {
                            Description = usage.Description,
                            Type = usage.Type,
                            Amount = usage.Amount,
                            Rate = usage.Rate
                        });
                    foreach (var charge in utility.Charges)
                        _charges.Add(new ChargeEditRow { Description = charge.Description, Amount = charge.Amount });
                    EnsureUsageTypes();
                    break;
                case ServiceStatement service:
                    foreach (var charge in service.Charges)
                        _charges.Add(new ChargeEditRow { Description = charge.Description, Amount = charge.Amount });
                    break;
            }
        }

        private void CopyPreviousPromotions()
        {
            if (_existingStatement is not null || gridPromotions is null)
                return;
            if (SelectedAccount() is not Account account)
                return;

            _promotions.Clear();
            foreach (var promo in PreviousStatementPromotions.CopyForNewStatement(_statementStore, account.Id))
            {
                _promotions.Add(ToEditRow(promo, dtStatementDate.Value.Date));
            }
        }

        private static PromotionEditRow ToEditRow(PromotionalBalance promo, DateTime fallbackAcquired) =>
            new()
            {
                Id = promo.Id,
                DateAcquired = promo.DateAcquired.Year > 1 ? promo.DateAcquired.Date : fallbackAcquired,
                InitialAmount = promo.InitialAmount > 0 ? promo.InitialAmount : promo.CurrentBalance,
                CurrentBalance = promo.CurrentBalance,
                Deadline = SafeDate(promo.Deadline),
                Type = promo.Type
            };

        private void OnAddPromotion(object? sender, EventArgs e) =>
            _promotions.Add(new PromotionEditRow
            {
                DateAcquired = dtStatementDate.Value.Date,
                Deadline = DateTime.Today.AddMonths(1),
                Type = PromoType.LumpSum
            });

        private void AttachPromotionMenu(StatementEntryGrid grid)
        {
            var menu = PromotionMenu(grid);
            grid.Disposed += (_, _) =>
            {
                if (!menu.IsDisposed)
                    menu.Dispose();
            };
            grid.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    ShowPromotionMenu(grid, menu, e.Location);
            };
            grid.EditingControlShowing += (_, e) =>
            {
                if (!menu.IsDisposed && !ReferenceEquals(e.Control.ContextMenuStrip, menu))
                    e.Control.ContextMenuStrip = menu;
            };
        }

        private static void ShowPromotionMenu(StatementEntryGrid grid, ContextMenuStrip menu, Point location)
        {
            if (grid.IsDisposed || menu.IsDisposed)
                return;

            var hit = grid.HitTest(location.X, location.Y);
            if (hit.RowIndex < 0 || grid.Rows[hit.RowIndex].DataBoundItem is not PromotionEditRow)
                return;

            if (grid.IsCurrentCellInEditMode)
                grid.EndEdit();
            if (!DataGridViewUtil.SelectOnlyCell(grid, hit.RowIndex, hit.ColumnIndex))
                return;

            grid.BeginInvoke(() =>
            {
                if (!grid.IsDisposed && !menu.IsDisposed)
                    menu.Show(grid, location);
            });
        }

        private ContextMenuStrip PromotionMenu(StatementEntryGrid grid)
        {
            var menu = new ContextMenuStrip();
            var delete = new ToolStripMenuItem("Delete")
            {
                ShortcutKeyDisplayString = "Del"
            };
            delete.Click += (_, _) =>
            {
                if (!grid.IsDisposed)
                    grid.BeginInvoke(() => DeleteSelected(_promotions, grid));
            };
            menu.Items.Add(delete);
            GridCsvExport.AddCommand(menu, grid);
            return menu;
        }

        private void OnAddUsage(object? sender, EventArgs e)
        {
            gridUsage?.EndEdit();
            AppendUsageRow();
            (gridUsage as StatementEntryGrid)?.EditFirstCellOfLastRow();
        }

        private void OnDeleteUsage(object? sender, EventArgs e) =>
            DeleteSelected(_usage, gridUsage);

        private void OnAddCharge(object? sender, EventArgs e)
        {
            gridCharges?.EndEdit();
            AppendChargeRow();
            (gridCharges as StatementEntryGrid)?.EditFirstCellOfLastRow();
        }

        private void AppendUsageRow() =>
            _usage.Add(new UsageEditRow { Type = "kWh", Amount = 0, Rate = 0 });

        private void AppendChargeRow() =>
            _charges.Add(new ChargeEditRow());

        private void OnDeleteCharge(object? sender, EventArgs e) =>
            DeleteSelected(_charges, gridCharges);

        private void OnSave(object? sender, EventArgs e)
        {
            gridPromotions?.EndEdit();
            gridUsage?.EndEdit();
            gridCharges?.EndEdit();

            if (!TryBuildStatement(out var statement, out var error) || statement is null)
            {
                MessageBox.Show(this, error, "Statement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var findings = THMS.Logic.Finance.Planning.AccountStatementValidator.Validate(statement);
            if (findings.Count > 0)
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, findings), "Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (_orchestrator is not null)
                {
                    _orchestrator.SaveStatement(statement);
                }
                else
                {
                    _statementStore.Save(statement);
                }
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(this, "Statement", ex);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancel(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool TryBuildStatement(out AccountStatement? statement, out string error)
        {
            statement = null;
            error = "";
            if (SelectedAccount() is not Account account)
            {
                error = "Select an account.";
                return false;
            }

            if (SelectedType() is not StatementType type)
            {
                error = "This account does not support statements.";
                return false;
            }

            var amountDue = StatementAccountMatch.IsNonPayable(type) ? 0 : ReadAmountDue();

            statement = type switch
            {
                StatementType.Bank => new BankStatement
                {
                    StatementBalance = ValueOf(numStatementBalance)
                },
                StatementType.Investment => new InvestmentStatement
                {
                    StatementBalance = ValueOf(numStatementBalance)
                },
                StatementType.Loan => new LoanStatement
                {
                    StatementBalance = ValueOf(numStatementBalance)
                },
                StatementType.Mortgage => new MortgageStatement
                {
                    StatementBalance = ValueOf(numStatementBalance),
                    EscrowBalance = ValueOf(numEscrowBalance)
                },
                StatementType.CreditCard => new CreditCardStatement
                {
                    StatementBalance = ValueOf(numStatementBalance),
                    Promotions = _promotions.Select(p => new PromotionalBalance
                    {
                        Id = p.Id == Guid.Empty ? Guid.NewGuid() : p.Id,
                        AccountId = account.Id,
                        DateAcquired = p.DateAcquired.Date,
                        InitialAmount = p.InitialAmount > 0 ? p.InitialAmount : p.CurrentBalance,
                        CurrentBalance = p.CurrentBalance,
                        Deadline = p.Deadline.Date,
                        Type = p.Type
                    }).ToList()
                },
                StatementType.Utility => new UtilityStatement
                {
                    Usage = _usage.Select(u => new UtilityUsageRecord
                    {
                        Description = u.Description ?? "",
                        Type = u.Type ?? "",
                        Amount = u.Amount,
                        Rate = decimal.Round(u.Rate, 6, MidpointRounding.AwayFromZero)
                    }).ToList(),
                    Charges = _charges.Select(c => new UtilityChargeLine
                    {
                        Description = c.Description ?? "",
                        Amount = c.Amount
                    }).ToList()
                },
                StatementType.Service => new ServiceStatement
                {
                    Charges = _charges.Select(c => new ServiceChargeLine
                    {
                        Description = c.Description ?? "",
                        Amount = c.Amount
                    }).ToList()
                },
                StatementType.Insurance => new InsuranceStatement(),
                _ => null
            };

            if (statement is null)
            {
                error = "This account does not support statements.";
                return false;
            }

            statement.Id = _existingStatement?.Id ?? Guid.NewGuid();
            statement.AccountId = account.Id;
            statement.StatementDate = ReadStatementDate();
            statement.DueDate = StatementAccountMatch.IsNonPayable(type)
                ? statement.StatementDate
                : ReadDueDate();
            statement.AmountDue = amountDue;
            statement.BillCleared = _existingStatement?.BillCleared ?? false;
            statement.Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();
            return true;
        }

        private void EnsureUsageTypes()
        {
            if (gridUsage is null)
                return;
            foreach (DataGridViewColumn column in gridUsage.Columns)
            {
                if (column is not DataGridViewComboBoxColumn combo || combo.DataPropertyName != nameof(UsageEditRow.Type))
                    continue;
                var values = new List<string> { "kWh", "gallons", "GB", "minutes", "CCF" };
                foreach (var row in _usage)
                {
                    if (!string.IsNullOrWhiteSpace(row.Type) && !values.Contains(row.Type))
                        values.Add(row.Type);
                }

                combo.DataSource = values;
            }
        }

        private static void DeleteSelected<T>(BindingList<T> list, DataGridView? grid)
        {
            if (grid is null)
                return;
            if (grid.IsCurrentCellInEditMode)
                grid.EndEdit();
            if (grid.CurrentRow?.DataBoundItem is T item)
                list.Remove(item);
        }

        private Control AddBar(EventHandler add)
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = new Padding(0, Scale(6), 0, 0)
            };
            var button = new ThmsButton { Text = "Add", Margin = Padding.Empty };
            button.Click += add;
            bar.Controls.Add(button);
            return bar;
        }

        private static Control GridToolbar(string addText, string deleteText, EventHandler add, EventHandler delete)
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false
            };
            var addButton = new ThmsButton { Text = addText };
            var deleteButton = new ThmsButton { Text = deleteText, Destructive = true };
            addButton.Click += add;
            deleteButton.Click += delete;
            toolbar.Controls.Add(addButton);
            toolbar.Controls.Add(deleteButton);
            return toolbar;
        }

        private static StatementEntryGrid CreateGrid() => new();

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

        private static DataGridViewTextBoxColumn AmountColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.ValueType = typeof(decimal);
            column.DefaultCellStyle.Format = "n2";
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            return column;
        }

        private static DataGridViewTextBoxColumn RateColumn(string property, string header)
        {
            var column = AmountColumn(property, header);
            column.DefaultCellStyle.Format = "c6";
            return column;
        }

        private static DataGridViewTextBoxColumn CostColumn()
        {
            var column = AmountColumn(nameof(UsageEditRow.Cost), "Cost");
            column.DefaultCellStyle.Format = "c2";
            column.ReadOnly = true;
            return column;
        }

        private static CalendarColumn DatePickerColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                DefaultCellStyle = { Format = "d" }
            };

        private void QueueFitPromotionDateColumns()
        {
            if (gridPromotions is not { IsHandleCreated: true, IsDisposed: false } grid)
                return;

            grid.BeginInvoke(FitPromotionDateColumns);
        }

        private void FitPromotionDateColumns()
        {
            if (gridPromotions is not { IsHandleCreated: true, IsDisposed: false } grid)
                return;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column is not CalendarColumn || column.Index < 0)
                    continue;

                // Visible-row sizing runs before the grid is on screen and keeps the header width.
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                grid.AutoResizeColumn(column.Index, DataGridViewAutoSizeColumnMode.AllCells);
                var width = column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, fixedHeight: true);
                if (width > column.MinimumWidth)
                    column.MinimumWidth = width;
            }
        }

        private static DataGridViewComboBoxColumn PromoTypeColumn()
        {
            var choices = new List<PromoTypeOption>
            {
                new(PromoTypeDisplay.Name(PromoType.LumpSum), PromoType.LumpSum),
                new(PromoTypeDisplay.Name(PromoType.EqualPayments), PromoType.EqualPayments)
            };
            return new DataGridViewComboBoxColumn
            {
                DataPropertyName = nameof(PromotionEditRow.Type),
                HeaderText = "Promo Type",
                Name = nameof(PromotionEditRow.Type),
                DataSource = choices,
                DisplayMember = nameof(PromoTypeOption.Name),
                ValueMember = nameof(PromoTypeOption.Type),
                ValueType = typeof(PromoType),
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                ToolTipText = "Lump sum: pay any amounts by the deadline. Equal payments: remaining balance divided across months until the deadline."
            };
        }

        private static DataGridViewComboBoxColumn ComboColumn(string property, string header, IReadOnlyList<string> values) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                DataSource = values.ToList(),
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };

        private static NumericUpDown MoneyBox(bool allowNegative = false) =>
            new()
            {
                DecimalPlaces = 2,
                Maximum = 100_000_000,
                Minimum = allowNegative ? -100_000_000 : 0,
                ThousandsSeparator = true,
                TextAlign = HorizontalAlignment.Right
            };

        private static void SetMoney(NumericUpDown? box, decimal value)
        {
            if (box is null)
                return;
            box.Value = Math.Clamp(value, box.Minimum, box.Maximum);
        }

        private static decimal ValueOf(NumericUpDown? box) => box?.Value ?? 0;

        private DateTime ReadStatementDate() =>
            ReadDate(dtStatementDate, _chosenStatementDate);

        private DateTime ReadDueDate() =>
            ReadDate(dtDueDate, _chosenDueDate);

        private decimal ReadAmountDue() =>
            ReadMoney(numAmountDue, _chosenAmountDue);

        // DateTimePicker and NumericUpDown keep typed text out of Value until
        // focus leaves. A later rebuild can also put the suggested dates back.
        // Prefer the text the user still sees, then the value they already chose.
        private static DateTime ReadDate(DateTimePicker picker, DateTime? chosen)
        {
            if (DateTime.TryParse(picker.Text, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var typed)
                && typed.Date != picker.Value.Date
                && typed >= picker.MinDate
                && typed <= picker.MaxDate)
                return typed.Date;

            return (chosen ?? picker.Value).Date;
        }

        private static decimal ReadMoney(NumericUpDown box, decimal? chosen)
        {
            if (decimal.TryParse(box.Text, NumberStyles.Currency, CultureInfo.CurrentCulture, out var typed)
                && typed != box.Value)
                return Math.Clamp(typed, box.Minimum, box.Maximum);

            return chosen ?? box.Value;
        }

        private static DateTime SafeDate(DateTime value) =>
            value == default ? DateTime.Today : value;

        private sealed class PromoTypeOption(string name, PromoType type)
        {
            public string Name { get; } = name;
            public PromoType Type { get; } = type;
        }

        private sealed class AccountListItem(Account account)
        {
            public Account Account { get; } = account;
            public Guid Id => Account.Id;
            public string Label => $"{Account.Name} ({AccountKinds.Of(Account)})";
        }

        private sealed class PromotionEditRow : INotifyPropertyChanged
        {
            private DateTime _dateAcquired = DateTime.Today;
            private decimal _initialAmount;
            private decimal _currentBalance;
            private DateTime _deadline = DateTime.Today;
            private PromoType _type;

            public Guid Id { get; set; }

            public DateTime DateAcquired
            {
                get => _dateAcquired;
                set => Set(ref _dateAcquired, value.Date);
            }

            public decimal InitialAmount
            {
                get => _initialAmount;
                set => Set(ref _initialAmount, value);
            }

            public decimal CurrentBalance
            {
                get => _currentBalance;
                set => Set(ref _currentBalance, value);
            }

            public DateTime Deadline
            {
                get => _deadline;
                set => Set(ref _deadline, value.Date);
            }

            public PromoType Type
            {
                get => _type;
                set => Set(ref _type, value);
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            private void Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                    return;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private sealed class UsageEditRow : INotifyPropertyChanged
        {
            private decimal _amount;
            private decimal _rate;

            public string Description { get; set; } = "";
            public string Type { get; set; } = "kWh";

            public decimal Amount
            {
                get => _amount;
                set
                {
                    if (_amount == value)
                        return;
                    _amount = value;
                    Notify(nameof(Amount));
                    Notify(nameof(Cost));
                }
            }

            public decimal Rate
            {
                get => _rate;
                set
                {
                    var rate = decimal.Round(value, 6, MidpointRounding.AwayFromZero);
                    if (_rate == rate)
                        return;
                    _rate = rate;
                    Notify(nameof(Rate));
                    Notify(nameof(Cost));
                }
            }

            public decimal Cost => new UtilityUsageRecord { Amount = Amount, Rate = Rate }.Cost;

            public event PropertyChangedEventHandler? PropertyChanged;

            private void Notify(string name) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private sealed class ChargeEditRow
        {
            public string Description { get; set; } = "";
            public decimal Amount { get; set; }
        }
    }
}
