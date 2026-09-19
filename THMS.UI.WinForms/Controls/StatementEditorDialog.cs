using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class StatementEditorDialog : Form
    {
        private readonly IAccountStatementDataStore _statementStore;
        private readonly PlanningOrchestrator? _orchestrator;
        private readonly AccountStatement? _existingStatement;
        private readonly Account? _lockedAccount;
        private readonly List<Account> _accounts;
        private Guid? _pendingPayFromId;
        private bool _loading;

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
            if (existingStatement is not null)
                _pendingPayFromId = orchestrator.FundingAccountForStatement(existingStatement.Id);
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
            WireComboDrawing();
            _loading = true;
            BindStatementTypes();
            LoadExisting();
            ApplyLockedAccountUi();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            SizeDialogInputs();
            FitToWorkingArea();
            ShowObligationFields(SelectedType() is StatementType type && type != StatementType.Bank);
        }

        private void WireComboDrawing()
        {
            foreach (var combo in new[] { cboAccount, cboStatementType, cboPayFrom })
            {
                combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.IntegralHeight = false;
                combo.DrawItem -= OnComboDrawItem;
                combo.DrawItem += OnComboDrawItem;
            }
        }

        private void SizeDialogInputs()
        {
            var comboHeight = Math.Max(LogicalToDeviceUnits(32), Font.Height + LogicalToDeviceUnits(14));
            var itemHeight = Math.Max(LogicalToDeviceUnits(20), Font.Height);
            foreach (var combo in new[] { cboAccount, cboStatementType, cboPayFrom })
            {
                combo.ItemHeight = itemHeight;
                combo.Height = comboHeight;
            }

            var typeRows = cboStatementType.Visible ? 2 : 1;
            typeSelectorLayout.RowStyles[0].SizeType = SizeType.Absolute;
            typeSelectorLayout.RowStyles[0].Height = cboStatementType.Visible ? comboHeight : 0;
            typeSelectorLayout.RowStyles[1].SizeType = SizeType.Absolute;
            typeSelectorLayout.RowStyles[1].Height = comboHeight;
            layout.RowStyles[0].SizeType = SizeType.Absolute;
            layout.RowStyles[0].Height = comboHeight * typeRows + LogicalToDeviceUnits(8);

            var fieldHeight = Math.Max(LogicalToDeviceUnits(36), Font.Height + LogicalToDeviceUnits(16));
            pnlCommon.RowStyles[0].Height = fieldHeight;
            if (txtAmountDue.Visible)
            {
                pnlCommon.RowStyles[1].Height = fieldHeight;
                pnlCommon.RowStyles[2].Height = fieldHeight;
            }
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
            var type = SelectedType();
            IEnumerable<Account> source = _accounts;
            if (type is StatementType statementType)
                source = _accounts.Where(a => StatementAccountMatch.Matches(a, statementType));

            var items = source
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
            BindPayFrom();
        }

        private void ApplyLockedAccountUi()
        {
            if (_lockedAccount is null)
                return;

            lblStatementType.Visible = false;
            cboStatementType.Visible = false;
            btnNewAccount.Visible = false;
            cboAccount.Enabled = false;
            typeSelectorLayout.RowStyles[0].SizeType = SizeType.Absolute;
            typeSelectorLayout.RowStyles[0].Height = 0;
            if (IsHandleCreated)
                SizeDialogInputs();
            else
                layout.RowStyles[0].Height = 44F;
        }

        private void BindStatementTypes()
        {
            cboStatementType.DisplayMember = nameof(StatementTypeOption.Name);
            cboStatementType.ValueMember = nameof(StatementTypeOption.Type);
            cboStatementType.DataSource = new List<StatementTypeOption>
            {
                new("Loan", StatementType.Loan),
                new("Mortgage", StatementType.Mortgage),
                new("Credit Card", StatementType.CreditCard),
                new("Utility", StatementType.Utility),
                new("Service", StatementType.Service),
                new("Insurance", StatementType.Insurance),
                new("Bank", StatementType.Bank)
            };
            cboStatementType.SelectedIndex = -1;
        }

        private void LoadExisting()
        {
            _loading = true;
            dtStatementDate.Value = DateTime.Today.AddDays(-15);
            dtDueDate.Value = DateTime.Today.AddDays(15);
            txtAmountDue.Text = "";

            if (_existingStatement is null)
            {
                if (_lockedAccount is Account locked
                    && StatementAccountMatch.ForAccount(locked) is StatementType lockedType)
                {
                    SelectType(lockedType);
                    BindAccounts(locked.Id);
                    _loading = false;
                    LoadTypePanel(lockedType);
                    CopyPreviousPromotions();
                    return;
                }

                cboStatementType.SelectedIndex = -1;
                BindAccounts();
                _loading = false;
                pnlTypeSpecific.Controls.Clear();
                ShowObligationFields(false);
                return;
            }

            SelectType(_existingStatement.Type);
            BindAccounts(_existingStatement.AccountId);
            dtStatementDate.Value = SafeDate(_existingStatement.StatementDate);
            dtDueDate.Value = SafeDate(_existingStatement.DueDate);
            txtNotes.Text = _existingStatement.Notes ?? "";
            if (_existingStatement is not BankStatement)
            {
                txtAmountDue.Text = _existingStatement.AmountDue.ToString("0.00");
            }

            _loading = false;
            LoadTypePanel(_existingStatement.Type);
            BindTypeSpecific(_existingStatement);
        }

        private void SelectType(StatementType type)
        {
            foreach (StatementTypeOption option in cboStatementType.Items)
            {
                if (option.Type == type)
                {
                    cboStatementType.SelectedItem = option;
                    return;
                }
            }
        }

        private void OnStatementTypeChanged(object? sender, EventArgs e)
        {
            if (_loading)
                return;

            var keep = SelectedAccount()?.Id;
            BindAccounts(keep);
            if (SelectedType() is not StatementType type)
            {
                pnlTypeSpecific.Controls.Clear();
                ShowObligationFields(false);
                return;
            }

            LoadTypePanel(type);
            CopyPreviousPromotions();
        }

        private void OnNewAccount(object? sender, EventArgs e)
        {
            var seed = SelectedType() is StatementType type
                ? StatementAccountMatch.CreateAccount(type)
                : new UntrackedAccount { Type = AccountType.Utility };

            using var dlg = new AccountEditForm(seed);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                new AccountOrchestrator().Save(dlg.Account);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _accounts.RemoveAll(a => a.Id == dlg.Account.Id);
            _accounts.Add(dlg.Account);

            _loading = true;
            if (SelectedType() is null && StatementAccountMatch.ForAccount(dlg.Account) is StatementType inferred)
                SelectType(inferred);
            BindAccounts(dlg.Account.Id);
            _loading = false;
            if (dlg.Account is BankAccount or CreditAccount)
                BindPayFrom(dlg.Account.Id);
            if (SelectedType() is StatementType selected)
            {
                LoadTypePanel(selected);
                CopyPreviousPromotions();
            }
        }

        private StatementType? SelectedType() =>
            cboStatementType.SelectedItem is StatementTypeOption option ? option.Type : null;

        private Account? SelectedAccount() =>
            cboAccount.SelectedItem is AccountListItem item ? item.Account : null;

        private void OnStatementAccountChanged(object? sender, EventArgs e)
        {
            if (_loading)
                return;
            BindPayFrom();
            CopyPreviousPromotions();
        }

        private void BindPayFrom(Guid? selectedId = null)
        {
            var keep = selectedId ?? _pendingPayFromId ?? SelectedPayFrom()?.Id;
            var exclude = SelectedAccount()?.Id;
            var items = _accounts
                .Where(a => a is BankAccount or CreditAccount)
                .Where(a => exclude is null || a.Id != exclude)
                .OrderBy(a => a.Name)
                .Select(a => new AccountListItem(a))
                .ToList();

            cboPayFrom.DisplayMember = nameof(AccountListItem.Label);
            cboPayFrom.ValueMember = nameof(AccountListItem.Id);
            cboPayFrom.DataSource = items;

            if (keep is Guid id)
            {
                var match = items.FirstOrDefault(i => i.Id == id);
                cboPayFrom.SelectedItem = match;
                if (match is null)
                    cboPayFrom.SelectedIndex = -1;
                else
                    _pendingPayFromId = null;
            }
            else
            {
                cboPayFrom.SelectedIndex = -1;
            }
        }

        private Account? SelectedPayFrom() =>
            cboPayFrom.SelectedItem is AccountListItem item ? item.Account : null;

        private void LoadTypePanel(StatementType type)
        {
            pnlTypeSpecific.SuspendLayout();
            pnlTypeSpecific.Controls.Clear();
            numEscrowBalance = null;
            numStatementBalance = null;
            gridPromotions = null;
            gridUsage = null;
            gridCharges = null;
            _promotions = [];
            _usage = [];
            _charges = [];

            Control panel = type switch
            {
                StatementType.Bank => BuildBankPanel(),
                StatementType.Loan => BuildLoanPanel(),
                StatementType.Mortgage => BuildMortgagePanel(),
                StatementType.CreditCard => BuildCreditCardPanel(),
                StatementType.Utility => BuildUtilityPanel(),
                StatementType.Service => BuildServicePanel(),
                _ => new Panel()
            };
            panel.Dock = UsesFixedFields(type) ? DockStyle.Top : DockStyle.Fill;
            pnlTypeSpecific.Controls.Add(panel);
            pnlTypeSpecific.ResumeLayout();
            ShowObligationFields(type != StatementType.Bank);
        }

        private static bool UsesFixedFields(StatementType type) =>
            type is StatementType.Bank or StatementType.Loan or StatementType.Mortgage;

        private void ShowObligationFields(bool visible)
        {
            lblDueDate.Visible = visible;
            dtDueDate.Visible = visible;
            lblAmountDue.Visible = visible;
            txtAmountDue.Visible = visible;
            lblPayFrom.Visible = visible;
            cboPayFrom.Visible = visible;

            var rowHeight = IsHandleCreated
                ? Math.Max(LogicalToDeviceUnits(36), Font.Height + LogicalToDeviceUnits(16))
                : 36;
            pnlCommon.RowStyles[1].Height = visible ? rowHeight : 0;
            pnlCommon.RowStyles[2].Height = visible ? rowHeight : 0;
            if (visible)
                BindPayFrom();
            if (IsHandleCreated)
                SizeDialogInputs();
        }

        private Control BuildBankPanel()
        {
            var layout = FieldLayout(1);
            layout.Name = "pnlBankStatement";
            numStatementBalance = MoneyBox();
            AddField(layout, 0, "Statement Balance", numStatementBalance);
            return layout;
        }

        private Control BuildLoanPanel()
        {
            var layout = FieldLayout(1);
            numStatementBalance = MoneyBox();
            AddField(layout, 0, "Statement Balance", numStatementBalance);
            return layout;
        }

        private Control BuildMortgagePanel()
        {
            var layout = FieldLayout(2);
            numStatementBalance = MoneyBox();
            numEscrowBalance = MoneyBox();
            AddField(layout, 0, "Statement Balance", numStatementBalance);
            AddField(layout, 1, "Escrow Balance", numEscrowBalance);
            return layout;
        }

        private Control BuildCreditCardPanel()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var fields = FieldLayout(1);
            numStatementBalance = MoneyBox();
            AddField(fields, 0, "Statement Balance", numStatementBalance);
            fields.Dock = DockStyle.Top;
            fields.Height = 36;

            gridPromotions = CreateGrid();
            gridPromotions.Columns.Add(DatePickerColumn(nameof(PromotionEditRow.DateAcquired), "Date Acquired"));
            gridPromotions.Columns.Add(AmountColumn(nameof(PromotionEditRow.InitialAmount), "Initial Amount"));
            gridPromotions.Columns.Add(AmountColumn(nameof(PromotionEditRow.CurrentBalance), "Current Balance"));
            gridPromotions.Columns.Add(DatePickerColumn(nameof(PromotionEditRow.Deadline), "Deadline"));
            gridPromotions.Columns.Add(PromoTypeColumn());
            gridPromotions.DataSource = _promotions;

            layout.Controls.Add(fields, 0, 0);
            layout.Controls.Add(GridToolbar("Add Promotion", "Delete Promotion", OnAddPromotion, OnDeletePromotion), 0, 1);
            layout.Controls.Add(gridPromotions, 0, 2);
            return layout;
        }

        private Control BuildUtilityPanel()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            gridUsage = CreateGrid();
            gridUsage.Columns.Add(ComboColumn(
                nameof(UsageEditRow.Type),
                "Type",
                ["kWh", "gallons", "GB", "minutes"]));
            gridUsage.Columns.Add(AmountColumn(nameof(UsageEditRow.Amount), "Amount"));
            gridUsage.Columns.Add(AmountColumn(nameof(UsageEditRow.Rate), "Rate"));
            gridUsage.DataSource = _usage;

            gridCharges = CreateGrid();
            gridCharges.Columns.Add(TextColumn(nameof(ChargeEditRow.Description), "Description"));
            gridCharges.Columns.Add(AmountColumn(nameof(ChargeEditRow.Amount), "Amount"));
            gridCharges.DataSource = _charges;

            layout.Controls.Add(GridToolbar("Add Usage", "Delete Usage", OnAddUsage, OnDeleteUsage), 0, 0);
            layout.Controls.Add(gridUsage, 0, 1);
            layout.Controls.Add(GridToolbar("Add Charge", "Delete Charge", OnAddCharge, OnDeleteCharge), 0, 2);
            layout.Controls.Add(gridCharges, 0, 3);
            return layout;
        }

        private Control BuildServicePanel()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            gridCharges = CreateGrid();
            gridCharges.Columns.Add(TextColumn(nameof(ChargeEditRow.Description), "Description"));
            gridCharges.Columns.Add(AmountColumn(nameof(ChargeEditRow.Amount), "Amount"));
            gridCharges.DataSource = _charges;

            layout.Controls.Add(GridToolbar("Add Charge", "Delete Charge", OnAddCharge, OnDeleteCharge), 0, 0);
            layout.Controls.Add(gridCharges, 0, 1);
            return layout;
        }

        private void BindTypeSpecific(AccountStatement statement)
        {
            switch (statement)
            {
                case BankStatement bank:
                    SetMoney(numStatementBalance, bank.StatementBalance);
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
                        _usage.Add(new UsageEditRow { Type = usage.Type, Amount = usage.Amount, Rate = usage.Rate });
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

        private void OnDeletePromotion(object? sender, EventArgs e) =>
            DeleteSelected(_promotions, gridPromotions);

        private void OnAddUsage(object? sender, EventArgs e) =>
            _usage.Add(new UsageEditRow { Type = "kWh", Amount = 0, Rate = 0 });

        private void OnDeleteUsage(object? sender, EventArgs e) =>
            DeleteSelected(_usage, gridUsage);

        private void OnAddCharge(object? sender, EventArgs e) =>
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
                    _orchestrator.EnsureStatementPayment(statement, SelectedPayFrom()?.Id);
                }
                else
                {
                    _statementStore.Save(statement);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Statement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                error = "Select a statement type.";
                return false;
            }

            decimal amountDue = 0;
            if (type != StatementType.Bank &&
                !TryParseMoney(txtAmountDue.Text, "Amount due", out amountDue, out error))
                return false;

            if (type != StatementType.Bank && amountDue > 0 && SelectedPayFrom() is null)
            {
                error = "Select the account to pay this statement from.";
                return false;
            }

            statement = type switch
            {
                StatementType.Bank => new BankStatement
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
                        Type = u.Type ?? "",
                        Amount = u.Amount,
                        Rate = u.Rate
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
                error = "Select a statement type.";
                return false;
            }

            statement.Id = _existingStatement?.Id ?? Guid.NewGuid();
            statement.AccountId = account.Id;
            statement.StatementDate = dtStatementDate.Value.Date;
            statement.DueDate = type == StatementType.Bank
                ? statement.StatementDate
                : dtDueDate.Value.Date;
            statement.AmountDue = amountDue;
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
                var values = new List<string> { "kWh", "gallons", "GB", "minutes" };
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
            if (grid?.CurrentRow?.DataBoundItem is T item)
                list.Remove(item);
        }

        private static TableLayoutPanel FieldLayout(int rows)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                ColumnCount = 2,
                RowCount = rows,
                Padding = new Padding(0, 4, 0, 4)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (var i = 0; i < rows; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            return layout;
        }

        private static void AddField(TableLayoutPanel layout, int row, string label, Control field)
        {
            layout.Controls.Add(new Label
            {
                Text = label,
                AutoSize = false,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            }, 0, row);
            field.Dock = DockStyle.Fill;
            layout.Controls.Add(field, 1, row);
        }

        private static Control GridToolbar(string addText, string deleteText, EventHandler add, EventHandler delete)
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false
            };
            var addButton = new Button { Text = addText, AutoSize = true };
            var deleteButton = new Button { Text = deleteText, AutoSize = true };
            addButton.Click += add;
            deleteButton.Click += delete;
            toolbar.Controls.Add(addButton);
            toolbar.Controls.Add(deleteButton);
            return toolbar;
        }

        private static DataGridView CreateGrid()
        {
            return new StatementEntryGrid();
        }

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

        private static CalendarColumn DatePickerColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };

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
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };

        private static NumericUpDown MoneyBox(bool allowNegative = false) =>
            new()
            {
                DecimalPlaces = 2,
                Maximum = 100_000_000,
                Minimum = allowNegative ? -100_000_000 : 0,
                ThousandsSeparator = true
            };

        private static void SetMoney(NumericUpDown? box, decimal value)
        {
            if (box is null)
                return;
            box.Value = Math.Clamp(value, box.Minimum, box.Maximum);
        }

        private static decimal ValueOf(NumericUpDown? box) => box?.Value ?? 0;

        private static bool TryParseMoney(string text, string field, out decimal value, out string error)
        {
            if (decimal.TryParse(text, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.AllowCurrencySymbol,
                    System.Globalization.CultureInfo.CurrentCulture, out value))
            {
                error = "";
                return true;
            }

            value = 0;
            error = $"{field} must be a number.";
            return false;
        }

        private static DateTime SafeDate(DateTime value) =>
            value == default ? DateTime.Today : value;

        private sealed class StatementTypeOption(string name, StatementType type)
        {
            public string Name { get; } = name;
            public StatementType Type { get; } = type;
        }

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

        private sealed class UsageEditRow
        {
            public string Type { get; set; } = "kWh";
            public decimal Amount { get; set; }
            public decimal Rate { get; set; }
        }

        private sealed class ChargeEditRow
        {
            public string Description { get; set; } = "";
            public decimal Amount { get; set; }
        }
    }
}
