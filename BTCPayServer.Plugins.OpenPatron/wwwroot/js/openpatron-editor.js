(function () {
    var config = window.__openpatron || {};
    var DEFAULT_ACCENT = config.defaultAccent || '#6366f1';
    var DEFAULT_BORDER_RADIUS = config.defaultBorderRadius || '1.5rem';
    var DEFAULT_BLOCK_SPACING = config.defaultBlockSpacing || '1rem';
    var BLOCK_TYPES = config.blockTypes || {};

    var BLOCK_FORMS = {
        'profile-hero': {
            fields: [
                { key: 'DisplayName', label: 'Display name', type: 'text', placeholder: 'Jane Doe' },
                { key: 'Subtitle', label: 'Subtitle', type: 'text', placeholder: 'Open source maintainer' },
                { key: 'Bio', label: 'Bio', type: 'textarea', placeholder: 'A few words about yourself\u2026' },
                { key: 'GravatarEmail', label: 'Gravatar email', type: 'email', placeholder: 'you@example.com' },
                { key: 'GitHubUsername', label: 'GitHub username', type: 'text', placeholder: 'octocat' },
                { key: 'SocialX', label: 'X (Twitter)', type: 'text', placeholder: 'handle' },
                { key: 'SocialMastodon', label: 'Mastodon URL', type: 'url', placeholder: 'https://mastodon.social/@@user' },
                { key: 'SocialNostr', label: 'Nostr (npub)', type: 'text', placeholder: 'npub1\u2026' }
            ]
        },
        'project-hero': {
            fields: [
                { key: 'Title', label: 'Headline', type: 'text', placeholder: 'Support My Work' },
                { key: 'Subtitle', label: 'Subtitle', type: 'text', placeholder: 'Help fund open source development' },
                { key: 'DisplayName', label: 'Maintainer name', type: 'text', placeholder: 'Jane Doe' },
                { key: 'GravatarEmail', label: 'Gravatar email', type: 'email', placeholder: 'you@example.com' },
                { key: 'GitHubUsername', label: 'GitHub username', type: 'text', placeholder: 'octocat' },
                { key: 'SocialX', label: 'X (Twitter)', type: 'text', placeholder: 'handle' },
                { key: 'SocialMastodon', label: 'Mastodon URL', type: 'url', placeholder: 'https://mastodon.social/@@user' },
                { key: 'SocialNostr', label: 'Nostr (npub)', type: 'text', placeholder: 'npub1\u2026' }
            ]
        },
        'funding-progress': {
            fields: [
                { key: 'Goal', label: 'Funding goal amount', type: 'number', placeholder: '1000' }
            ]
        },
        'description': {
            fields: [
                { key: 'Heading', label: 'Section heading', type: 'text', placeholder: 'Why sponsor this work?' },
                { key: 'Content', label: 'Content', type: 'textarea', placeholder: 'Tell visitors why they should sponsor\u2026', rows: 8 }
            ]
        },
        'projects-grid': {
            custom: true,
            extraFields: [
                { key: 'ColumnsPerRow', label: 'Items per row', type: 'select', options: [{v:'1',l:'1'},{v:'2',l:'2'},{v:'3',l:'3'},{v:'4',l:'4'}], default: '2' }
            ]
        },
        'subscription-tiers': {
            fields: [
                { key: 'Heading', label: 'Section heading', type: 'text', placeholder: 'Choose a sponsorship tier' },
                { key: 'Subtitle', label: 'Subtitle', type: 'text', placeholder: 'Pick the level that fits you best' },
                { key: 'ColumnsPerRow', label: 'Items per row', type: 'select', options: [{v:'1',l:'1'},{v:'2',l:'2'},{v:'3',l:'3'},{v:'4',l:'4'}], default: '2' }
            ]
        },
        'quick-support': {
            fields: [
                { key: 'Heading', label: 'Section heading', type: 'text', placeholder: 'Send quick support' },
                { key: 'SuggestedAmounts', label: 'Suggested amounts (comma-separated)', type: 'text', placeholder: '5, 15, 50' }
            ]
        },
        'sponsor-wall': {
            fields: [
                { key: 'Heading', label: 'Section heading', type: 'text', placeholder: "Who's supporting this work" }
            ]
        },
        'sidebar-support': {
            fields: [
                { key: 'Heading', label: 'Section heading', type: 'text', placeholder: 'Sponsor now' }
            ]
        }
    };

    var LAYOUT_PRESETS = { '8-4': [8, 4], '4-8': [4, 8], '6-6': [6, 6], '12': [12] };

    var sectionsInput = document.getElementById('sectionsJson');
    var presetInput = document.getElementById('pageLayoutPreset');
    var sectionPreview = document.getElementById('sectionPreview');
    var emptyState = document.getElementById('emptyBlockState');
    var blockCountEl = document.getElementById('blockCount');
    var visualEditor = document.getElementById('visualEditor');
    var jsonEditor = document.getElementById('jsonEditor');
    var jsonTextarea = document.getElementById('jsonEditorTextarea');
    var jsonError = document.getElementById('jsonEditorError');
    var modeVisualBtn = document.getElementById('modeVisualBtn');
    var modeJsonBtn = document.getElementById('modeJsonBtn');
    var modalEl = document.getElementById('blockEditModal');
    var modalBody = document.getElementById('blockEditModalBody');
    var modalTitle = document.getElementById('blockEditModalTitle');

    var layoutPreset = presetInput.value || '8-4';
    var sections = [];
    try { sections = JSON.parse(sectionsInput.value); } catch (_) { sections = []; }
    if (!Array.isArray(sections) || !sections.length) sections = createSectionsFromPreset(layoutPreset);

    var activeColumnIdx = 0;
    var currentMode = 'visual';
    var sortableInstances = [];
    var editingSectionIdx = -1;
    var editingBlockIdx = -1;
    var modalInstance = null;

    function generateId() { return Math.random().toString(36).substring(2, 14); }

    function esc(s) {
        var d = document.createElement('div');
        d.textContent = s || '';
        return d.innerHTML;
    }

    function val(settings, key) {
        if (!settings || settings[key] == null) return '';
        return settings[key];
    }

    function createSectionsFromPreset(preset) {
        var widths = LAYOUT_PRESETS[preset] || [12];
        return widths.map(function (w, i) {
            return { Id: 'col-' + (i + 1), Width: w, Blocks: [] };
        });
    }

    function renderLayoutSelector() {
        var container = document.getElementById('layoutPresetSelector');
        container.innerHTML = '';
        var keys = Object.keys(LAYOUT_PRESETS);
        keys.forEach(function (key) {
            var widths = LAYOUT_PRESETS[key];
            var label = widths.join(' + ');
            if (key === '12') label = '12 (full width)';
            var isActive = layoutPreset === key;
            var colDiv = document.createElement('div');
            colDiv.className = 'col-6 col-sm-3';
            colDiv.innerHTML =
                '<div class="card text-center layout-preset-card' +
                (isActive ? ' border-primary border-2 bg-primary-subtle' : '') +
                '" data-preset="' + key + '" style="cursor:pointer">' +
                '<div class="card-body py-2 px-1">' +
                '<div class="d-flex gap-1 justify-content-center mb-1">' +
                widths.map(function (w) {
                    return '<div style="height:24px;flex:' + w +
                        ';background:' + (isActive ? 'var(--bs-primary)' : '#dee2e6') +
                        ';border-radius:3px"></div>';
                }).join('') +
                '</div>' +
                '<div class="small fw-semibold">' + esc(label) + '</div>' +
                '</div></div>';
            container.appendChild(colDiv);
        });
    }

    document.getElementById('layoutPresetSelector').addEventListener('click', function (e) {
        var card = e.target.closest('.layout-preset-card');
        if (!card) return;
        var newPreset = card.dataset.preset;
        if (newPreset === layoutPreset) return;
        var allBlocks = [];
        sections.forEach(function (s) { allBlocks = allBlocks.concat(s.Blocks || []); });

        layoutPreset = newPreset;
        presetInput.value = layoutPreset;
        var newSections = createSectionsFromPreset(layoutPreset);

        if (allBlocks.length > 0) {
            if (newSections.length === 1) {
                newSections[0].Blocks = allBlocks;
            } else {
                for (var i = 0; i < allBlocks.length; i++) {
                    var targetIdx = Math.min(i < sections.length ? i : 0, newSections.length - 1);
                    if (i < sections.length && i < newSections.length) {
                        newSections[i].Blocks = sections[i].Blocks || [];
                    }
                }
                var placed = {};
                newSections.forEach(function (s) { s.Blocks.forEach(function (b) { placed[b.Id] = true; }); });
                var unplaced = allBlocks.filter(function (b) { return !placed[b.Id]; });
                if (unplaced.length > 0) {
                    var widest = newSections.reduce(function (a, b) { return a.Width >= b.Width ? a : b; });
                    widest.Blocks = widest.Blocks.concat(unplaced);
                }
            }
        }

        sections = newSections;
        activeColumnIdx = 0;
        renderLayoutSelector();
        renderSections();
    });

    function renderSections() {
        sortableInstances.forEach(function (s) { s.destroy(); });
        sortableInstances = [];
        sectionPreview.innerHTML = '';
        var totalBlocks = 0;

        sections.forEach(function (section, sIdx) {
            var blocks = section.Blocks || [];
            totalBlocks += blocks.length;

            var col = document.createElement('div');
            col.className = 'col-' + section.Width;

            var isActive = sIdx === activeColumnIdx;
            var html =
                '<div class="border rounded-3 p-2 section-column' +
                (isActive ? ' border-primary' : '') +
                '" data-section-idx="' + sIdx + '" style="min-height:120px;background:#f8f9fa">';
            html +=
                '<div class="d-flex justify-content-between align-items-center mb-2">' +
                '<span class="fw-semibold small text-muted">Column (' + section.Width + ')</span>' +
                '</div>';
            html += '<div class="section-block-list" data-section-idx="' + sIdx + '">';

            blocks.forEach(function (block, bIdx) {
                var info = BLOCK_TYPES[block.Type] || { name: block.Type };
                var hasTheme = block.Theme && (block.Theme.AccentColor || block.Theme.BorderRadius);
                html +=
                    '<div class="card card-body p-2 mb-1 d-flex flex-row justify-content-between align-items-center block-card"' +
                    ' data-block-idx="' + bIdx + '" data-section-idx="' + sIdx + '" style="cursor:grab">' +
                    '<span class="small fw-semibold">' + esc(info.name) + '</span>' +
                    '<div class="d-flex align-items-center gap-1">' +
                    '<button type="button" class="btn btn-link btn-sm p-0 lh-1 block-theme-btn' +
                    (hasTheme ? ' text-primary' : ' text-muted') +
                    '" data-section-idx="' + sIdx + '" data-block-idx="' + bIdx +
                    '" title="Style overrides">&#9881;</button>' +
                    '<button type="button" class="btn btn-link btn-sm p-0 lh-1 text-danger block-remove-btn"' +
                    ' data-section-idx="' + sIdx + '" data-block-idx="' + bIdx +
                    '" title="Remove">&times;</button>' +
                    '</div></div>';
            });

            html += '</div>';
            html +=
                '<button type="button" class="btn btn-sm btn-outline-secondary w-100 mt-1 section-add-btn"' +
                ' data-section-idx="' + sIdx + '">+ Add block</button>';
            html += '</div>';

            col.innerHTML = html;
            sectionPreview.appendChild(col);

            var listEl = col.querySelector('.section-block-list');
            if (listEl && typeof Sortable !== 'undefined') {
                var sortable = Sortable.create(listEl, {
                    group: 'blocks',
                    animation: 150,
                    ghostClass: 'bg-primary-subtle',
                    onEnd: function (evt) {
                        var fromIdx = parseInt(evt.from.dataset.sectionIdx, 10);
                        var toIdx = parseInt(evt.to.dataset.sectionIdx, 10);
                        var oldIndex = evt.oldIndex;
                        var newIndex = evt.newIndex;
                        var moved = sections[fromIdx].Blocks.splice(oldIndex, 1)[0];
                        sections[toIdx].Blocks.splice(newIndex, 0, moved);
                        renderSections();
                    }
                });
                sortableInstances.push(sortable);
            }
        });

        emptyState.classList.toggle('d-none', totalBlocks > 0);
        blockCountEl.textContent = totalBlocks + ' block' + (totalBlocks !== 1 ? 's' : '');
        var swapBtn = document.getElementById('swapColumnsBtn');
        if (swapBtn) swapBtn.classList.toggle('d-none', sections.length !== 2);
        syncToInput();
    }

    document.getElementById('swapColumnsBtn').addEventListener('click', function (e) {
        e.preventDefault();
        if (sections.length !== 2) return;
        var tmp = sections[0].Blocks;
        sections[0].Blocks = sections[1].Blocks;
        sections[1].Blocks = tmp;
        renderSections();
    });

    sectionPreview.addEventListener('click', function (e) {
        var removeBtn = e.target.closest('.block-remove-btn');
        if (removeBtn) {
            e.preventDefault(); e.stopPropagation();
            var sIdx = parseInt(removeBtn.dataset.sectionIdx, 10);
            var bIdx = parseInt(removeBtn.dataset.blockIdx, 10);
            sections[sIdx].Blocks.splice(bIdx, 1);
            renderSections();
            return;
        }

        var themeBtn = e.target.closest('.block-theme-btn');
        if (themeBtn) {
            e.preventDefault(); e.stopPropagation();
            openBlockEditModal(
                parseInt(themeBtn.dataset.sectionIdx, 10),
                parseInt(themeBtn.dataset.blockIdx, 10)
            );
            return;
        }

        var addBtn = e.target.closest('.section-add-btn');
        if (addBtn) {
            e.preventDefault();
            activeColumnIdx = parseInt(addBtn.dataset.sectionIdx, 10);
            renderSections();
            var pickerCard = document.getElementById('blockPickerCard');
            if (pickerCard) {
                pickerCard.classList.add('border-primary', 'border-2');
                pickerCard.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                setTimeout(function () { pickerCard.classList.remove('border-primary', 'border-2'); }, 1500);
            }
            return;
        }

        var column = e.target.closest('.section-column');
        if (column && !e.target.closest('.block-card')) {
            activeColumnIdx = parseInt(column.dataset.sectionIdx, 10);
            renderSections();
        }
    });

    sectionPreview.addEventListener('dblclick', function (e) {
        var card = e.target.closest('.block-card');
        if (!card) return;
        e.preventDefault();
        openBlockEditModal(
            parseInt(card.dataset.sectionIdx, 10),
            parseInt(card.dataset.blockIdx, 10)
        );
    });

    function openBlockEditModal(sIdx, bIdx) {
        editingSectionIdx = sIdx;
        editingBlockIdx = bIdx;
        var block = sections[sIdx].Blocks[bIdx];
        if (!block) return;
        var info = BLOCK_TYPES[block.Type] || { name: block.Type };
        modalTitle.textContent = 'Edit: ' + info.name;
        modalBody.innerHTML = renderBlockFormFields(block) + renderBlockThemeFields(block);
        if (!modalInstance) modalInstance = new bootstrap.Modal(modalEl);
        modalInstance.show();
        wireProjectFormEvents();
    }

    function renderFieldHtml(f, block, s) {
        var v = val(s, f.key);
        if (f.key === 'SuggestedAmounts' && Array.isArray(v)) v = v.join(', ');
        if (!v && v !== 0 && f.default) v = f.default;
        var id = 'bf_' + block.Id + '_' + f.key;
        var html = '<div class="form-group mb-3">';
        html += '<label class="form-label" for="' + id + '">' + esc(f.label) + '</label>';
        if (f.type === 'select' && f.options) {
            html += '<select class="form-select block-field" id="' + id + '" data-key="' + f.key + '">';
            f.options.forEach(function (o) {
                var selected = (String(v) === String(o.v)) ? ' selected' : '';
                html += '<option value="' + esc(o.v) + '"' + selected + '>' + esc(o.l) + '</option>';
            });
            html += '</select>';
        } else if (f.type === 'textarea') {
            html += '<textarea class="form-control block-field" id="' + id + '" data-key="' + f.key +
                '" rows="' + (f.rows || 3) + '" placeholder="' + esc(f.placeholder) + '">' + esc(v) + '</textarea>';
        } else {
            html += '<input class="form-control block-field" id="' + id + '" data-key="' + f.key +
                '" type="' + (f.type || 'text') + '" placeholder="' + esc(f.placeholder || '') +
                '" value="' + esc(v) + '" />';
        }
        html += '</div>';
        return html;
    }

    function renderBlockFormFields(block) {
        var def = BLOCK_FORMS[block.Type];
        if (!def) return '<div class="text-muted small">No configuration for this block type.</div>';
        var s = block.Settings || {};

        if (def.custom && block.Type === 'projects-grid') {
            var html = renderProjectsForm(s);
            (def.extraFields || []).forEach(function (f) { html += renderFieldHtml(f, block, s); });
            return html;
        }

        var html = '';
        (def.fields || []).forEach(function (f) { html += renderFieldHtml(f, block, s); });
        return html;
    }

    function renderProjectsForm(s) {
        var projects = (s && s.Projects) || [];
        var html = '<div data-projects-form>';
        projects.forEach(function (p, i) {
            html +=
                '<div class="border rounded-3 p-2 mb-2 project-entry">' +
                '<div class="d-flex justify-content-between align-items-center mb-1">' +
                '<strong class="small">Project ' + (i + 1) + '</strong>' +
                '<button type="button" class="btn btn-sm btn-outline-danger py-0 px-1 project-entry-remove">&times;</button>' +
                '</div>' +
                '<div class="row g-1">' +
                '<div class="col-4"><input class="form-control form-control-sm proj-field" data-pkey="Name" placeholder="Name" value="' + esc(p.Name || '') + '" /></div>' +
                '<div class="col-4"><input class="form-control form-control-sm proj-field" data-pkey="Url" placeholder="URL" value="' + esc(p.Url || '') + '" /></div>' +
                '<div class="col-4"><input class="form-control form-control-sm proj-field" data-pkey="Description" placeholder="Description" value="' + esc(p.Description || '') + '" /></div>' +
                '</div></div>';
        });
        html += '<button type="button" class="btn btn-sm btn-outline-secondary project-add-btn">+ Add project</button>';
        html += '</div>';
        return html;
    }

    function renderBlockThemeFields(block) {
        var lo = block.Theme || {};
        var html = '<hr class="my-3"><h6>Theme overrides</h6>';
        html += '<div class="row g-2 align-items-end">';
        html += '<div class="col-auto">';
        html += '<label class="form-label small mb-0">Accent color</label>';
        html += '<div class="d-flex gap-1 align-items-center">';
        html += '<input type="color" class="form-control form-control-color form-control-sm modal-accent-picker" value="' + esc(lo.AccentColor || DEFAULT_ACCENT) + '" />';
        html += '<input type="text" class="form-control form-control-sm modal-theme-field" data-lkey="AccentColor" value="' + esc(lo.AccentColor || '') + '" placeholder="inherit" style="max-width:100px" />';
        html += '</div></div>';
        html += '<div class="col-auto">';
        html += '<label class="form-label small mb-0">Border radius</label>';
        html += '<select class="form-select form-select-sm modal-theme-field" data-lkey="BorderRadius">';
        html += '<option value=""' + (!lo.BorderRadius ? ' selected' : '') + '>Inherit</option>';
        html += '<option value="0"' + (lo.BorderRadius === '0' ? ' selected' : '') + '>Sharp (0)</option>';
        html += '<option value="0.5rem"' + (lo.BorderRadius === '0.5rem' ? ' selected' : '') + '>Subtle (0.5rem)</option>';
        html += '<option value="1rem"' + (lo.BorderRadius === '1rem' ? ' selected' : '') + '>Medium (1rem)</option>';
        html += '<option value="1.5rem"' + (lo.BorderRadius === '1.5rem' ? ' selected' : '') + '>Rounded (1.5rem)</option>';
        html += '<option value="2rem"' + (lo.BorderRadius === '2rem' ? ' selected' : '') + '>Very Rounded (2rem)</option>';
        html += '</select></div>';
        html += '<div class="col-auto"><button type="button" class="btn btn-sm btn-outline-secondary modal-theme-clear">Clear</button></div>';
        html += '</div>';
        return html;
    }

    modalBody.addEventListener('input', function (e) {
        var picker = e.target.closest('.modal-accent-picker');
        if (picker) {
            var textInput = modalBody.querySelector('.modal-theme-field[data-lkey="AccentColor"]');
            if (textInput) textInput.value = picker.value;
        }
        var tf = e.target.closest('.modal-theme-field[data-lkey="AccentColor"]');
        if (tf && /^#[0-9a-fA-F]{6}$/.test(tf.value)) {
            var p2 = modalBody.querySelector('.modal-accent-picker');
            if (p2) p2.value = tf.value;
        }
    });

    modalBody.addEventListener('click', function (e) {
        if (e.target.closest('.modal-theme-clear')) {
            var af = modalBody.querySelector('.modal-theme-field[data-lkey="AccentColor"]');
            var rf = modalBody.querySelector('.modal-theme-field[data-lkey="BorderRadius"]');
            if (af) af.value = '';
            if (rf) rf.value = '';
        }
    });

    document.getElementById('blockEditModalSave').addEventListener('click', function () {
        if (editingSectionIdx < 0 || editingBlockIdx < 0) return;
        var block = sections[editingSectionIdx].Blocks[editingBlockIdx];
        if (!block) return;

        var def = BLOCK_FORMS[block.Type];
        if (def) {
            if (!block.Settings) block.Settings = {};
            if (def.custom && block.Type === 'projects-grid') {
                var projects = [];
                modalBody.querySelectorAll('.project-entry').forEach(function (entry) {
                    var p = {};
                    entry.querySelectorAll('.proj-field').forEach(function (f) { p[f.dataset.pkey] = f.value; });
                    if (p.Name || p.Url) projects.push(p);
                });
                block.Settings.Projects = projects;
            } else if (def.fields) {
                modalBody.querySelectorAll('.block-field').forEach(function (input) {
                    var key = input.dataset.key;
                    var value = input.value;
                    if (key === 'SuggestedAmounts') {
                        block.Settings[key] = value.split(/[,;]/).map(function (s) { return parseFloat(s.trim()); }).filter(function (n) { return !isNaN(n) && n > 0; });
                    } else if (key === 'Goal' || key === 'ColumnsPerRow') {
                        block.Settings[key] = parseFloat(value) || 0;
                    } else {
                        block.Settings[key] = value;
                    }
                });
            }
        }

        var lo = {};
        modalBody.querySelectorAll('.modal-theme-field').forEach(function (f) {
            var v = f.value.trim();
            if (v) lo[f.dataset.lkey] = v;
        });
        block.Theme = (lo.AccentColor || lo.BorderRadius) ? lo : null;

        modalInstance.hide();
        editingSectionIdx = -1;
        editingBlockIdx = -1;
        renderSections();
    });

    function wireProjectFormEvents() {
        modalBody.querySelectorAll('.project-entry-remove').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                var entry = btn.closest('.project-entry');
                if (entry) entry.remove();
            });
        });

        var addBtn = modalBody.querySelector('.project-add-btn');
        if (addBtn) {
            addBtn.addEventListener('click', function (e) {
                e.preventDefault();
                var form = addBtn.closest('[data-projects-form]');
                var count = form.querySelectorAll('.project-entry').length;
                var newEntry = document.createElement('div');
                newEntry.className = 'border rounded-3 p-2 mb-2 project-entry';
                newEntry.innerHTML =
                    '<div class="d-flex justify-content-between align-items-center mb-1">' +
                    '<strong class="small">Project ' + (count + 1) + '</strong>' +
                    '<button type="button" class="btn btn-sm btn-outline-danger py-0 px-1 project-entry-remove">&times;</button>' +
                    '</div>' +
                    '<div class="row g-1">' +
                    '<div class="col-4"><input class="form-control form-control-sm proj-field" data-pkey="Name" placeholder="Name" /></div>' +
                    '<div class="col-4"><input class="form-control form-control-sm proj-field" data-pkey="Url" placeholder="URL" /></div>' +
                    '<div class="col-4"><input class="form-control form-control-sm proj-field" data-pkey="Description" placeholder="Description" /></div>' +
                    '</div>';
                form.insertBefore(newEntry, addBtn);
                newEntry.querySelector('.project-entry-remove').addEventListener('click', function () {
                    newEntry.remove();
                });
            });
        }
    }

    var ghUrlPattern = /^https?:\/\/github\.com\/([^\/]+)\/([^\/?#]+)\/?$/i;

    modalBody.addEventListener('blur', function (e) {
        var input = e.target;
        if (!input.matches || !input.matches('.proj-field[data-pkey="Url"]')) return;

        var url = input.value.trim();
        var match = ghUrlPattern.exec(url);
        if (!match) return;

        var owner = match[1];
        var repo = match[2].replace(/\.git$/i, '');
        var entry = input.closest('.project-entry');
        if (!entry) return;

        var nameField = entry.querySelector('.proj-field[data-pkey="Name"]');
        var descField = entry.querySelector('.proj-field[data-pkey="Description"]');

        if (nameField && nameField.value && descField && descField.value) return;

        input.classList.add('opacity-50');
        fetch('https://api.github.com/repos/' + encodeURIComponent(owner) + '/' + encodeURIComponent(repo), {
            headers: { 'Accept': 'application/vnd.github+json' }
        })
        .then(function (r) { return r.ok ? r.json() : null; })
        .then(function (data) {
            if (!data) return;
            if (nameField && !nameField.value) nameField.value = data.name || '';
            if (descField && !descField.value) descField.value = data.description || '';
        })
        .catch(function () {})
        .finally(function () { input.classList.remove('opacity-50'); });
    }, true);

    document.getElementById('blockPicker').addEventListener('click', function (e) {
        e.preventDefault();
        var item = e.target.closest('.block-picker-item');
        if (!item) return;
        var type = item.dataset.blockType;
        var defaults = {};
        var def = BLOCK_FORMS[type];
        if (def && def.fields) {
            def.fields.forEach(function (f) {
                if (f.key === 'SuggestedAmounts') defaults[f.key] = [];
                else if (f.key === 'Goal') defaults[f.key] = 0;
                else defaults[f.key] = '';
            });
        }
        if (type === 'projects-grid') defaults = { Projects: [] };
        var newBlock = { Id: generateId(), Type: type, Settings: defaults };
        var targetIdx = activeColumnIdx;
        if (targetIdx < 0 || targetIdx >= sections.length) targetIdx = 0;
        sections[targetIdx].Blocks.push(newBlock);
        renderSections();
    });

    function syncToInput() {
        sectionsInput.value = JSON.stringify(sections);
        presetInput.value = layoutPreset;
    }

    function getThemeFromForm() {
        return {
            AccentColor: document.getElementById('ThemeAccentColor').value || DEFAULT_ACCENT,
            BorderRadius: document.getElementById('ThemeBorderRadius').value || DEFAULT_BORDER_RADIUS,
            BlockSpacing: document.getElementById('ThemeBlockSpacing').value || DEFAULT_BLOCK_SPACING
        };
    }

    function setThemeToForm(theme) {
        if (!theme) return;
        var accentInput = document.getElementById('ThemeAccentColor');
        var radiusInput = document.getElementById('ThemeBorderRadius');
        var spacingInput = document.getElementById('ThemeBlockSpacing');
        if (theme.AccentColor && accentInput) accentInput.value = theme.AccentColor;
        if (theme.BorderRadius && radiusInput) radiusInput.value = theme.BorderRadius;
        if (theme.BlockSpacing && spacingInput) spacingInput.value = theme.BlockSpacing;
        var pickerEl = document.getElementById('accentColorPicker');
        if (pickerEl && theme.AccentColor && /^#[0-9a-fA-F]{6}$/.test(theme.AccentColor)) {
            pickerEl.value = theme.AccentColor;
        }
    }

    function switchToVisual() {
        if (currentMode === 'json') {
            try {
                var parsed = JSON.parse(jsonTextarea.value.trim());
                if (parsed && parsed.Sections) {
                    sections = parsed.Sections;
                    if (parsed.Layout) { layoutPreset = parsed.Layout; presetInput.value = layoutPreset; }
                    setThemeToForm(parsed.Theme);
                } else {
                    throw new Error('Must be an object with "Sections" array');
                }
                jsonError.classList.add('d-none');
            } catch (err) {
                jsonError.textContent = 'Invalid JSON: ' + err.message;
                jsonError.classList.remove('d-none');
                return;
            }
        }
        currentMode = 'visual';
        visualEditor.classList.remove('d-none');
        jsonEditor.classList.add('d-none');
        modeVisualBtn.classList.add('active');
        modeJsonBtn.classList.remove('active');
        renderLayoutSelector();
        renderSections();
    }

    function switchToJson() {
        currentMode = 'json';
        var payload = { Theme: getThemeFromForm(), Layout: layoutPreset, Sections: sections };
        jsonTextarea.value = JSON.stringify(payload, null, 2);
        jsonError.classList.add('d-none');
        visualEditor.classList.add('d-none');
        jsonEditor.classList.remove('d-none');
        modeVisualBtn.classList.remove('active');
        modeJsonBtn.classList.add('active');
    }

    modeVisualBtn.addEventListener('click', function (e) { e.preventDefault(); switchToVisual(); });
    modeJsonBtn.addEventListener('click', function (e) { e.preventDefault(); switchToJson(); });

    jsonTextarea.addEventListener('input', function () {
        try {
            var parsed = JSON.parse(jsonTextarea.value);
            if (parsed && parsed.Sections) {
                sections = parsed.Sections;
                if (parsed.Layout) { layoutPreset = parsed.Layout; presetInput.value = layoutPreset; }
                syncToInput();
                jsonError.classList.add('d-none');
            }
        } catch (_) {}
    });

    document.getElementById('openpatronForm').addEventListener('submit', function () {
        if (currentMode === 'json') {
            try {
                var p = JSON.parse(jsonTextarea.value);
                if (p && p.Sections) {
                    sections = p.Sections;
                    if (p.Layout) layoutPreset = p.Layout;
                    setThemeToForm(p.Theme);
                }
            } catch (_) {}
        }
        syncToInput();
    });

    renderLayoutSelector();
    renderSections();

    var picker = document.getElementById('accentColorPicker');
    var colorInput = document.getElementById('ThemeAccentColor');
    if (picker && colorInput) {
        picker.addEventListener('input', function () { colorInput.value = picker.value; });
        colorInput.addEventListener('input', function () {
            if (/^#[0-9a-fA-F]{6}$/.test(colorInput.value)) picker.value = colorInput.value;
        });
    }
})();
