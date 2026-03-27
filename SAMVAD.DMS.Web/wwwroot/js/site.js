(function () {
	function ensureConfirmModal() {
		var existing = document.getElementById('samvadConfirmModal');
		if (existing) {
			return existing;
		}

		var wrapper = document.createElement('div');
		wrapper.id = 'samvadConfirmModal';
		wrapper.className = 'fixed inset-0 z-50 hidden items-center justify-center bg-slate-900/50 p-4';
		wrapper.innerHTML =
			'<div class="w-full max-w-md rounded-2xl bg-white shadow-xl">' +
			'<div class="border-b border-slate-200 px-5 py-4">' +
			'<h3 id="samvadConfirmModalTitle" class="text-base font-semibold text-slate-900">Confirm Action</h3>' +
			'</div>' +
			'<div class="px-5 py-4">' +
			'<p id="samvadConfirmModalMessage" class="text-sm text-slate-600"></p>' +
			'</div>' +
			'<div class="flex justify-end gap-2 border-t border-slate-200 px-5 py-4">' +
			'<button type="button" id="samvadConfirmCancelBtn" class="rounded-lg border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700">Cancel</button>' +
			'<button type="button" id="samvadConfirmOkBtn" class="rounded-lg bg-rose-600 px-3 py-2 text-sm font-semibold text-white">Confirm</button>' +
			'</div>' +
			'</div>';

		document.body.appendChild(wrapper);
		return wrapper;
	}

	function closeModal(modal) {
		modal.classList.add('hidden');
		modal.classList.remove('flex');
	}

	function openModal(modal) {
		modal.classList.remove('hidden');
		modal.classList.add('flex');
	}

	function confirm(options) {
		var modal = ensureConfirmModal();
		var title = document.getElementById('samvadConfirmModalTitle');
		var message = document.getElementById('samvadConfirmModalMessage');
		var cancelBtn = document.getElementById('samvadConfirmCancelBtn');
		var okBtn = document.getElementById('samvadConfirmOkBtn');

		title.textContent = (options && options.title) ? options.title : 'Confirm Action';
		message.textContent = (options && options.message) ? options.message : 'Are you sure?';
		okBtn.textContent = (options && options.confirmText) ? options.confirmText : 'Confirm';
		okBtn.className = (options && options.confirmButtonClass)
			? options.confirmButtonClass
			: 'rounded-lg bg-rose-600 px-3 py-2 text-sm font-semibold text-white';

		return new Promise(function (resolve) {
			function cleanup() {
				cancelBtn.removeEventListener('click', onCancel);
				okBtn.removeEventListener('click', onConfirm);
				modal.removeEventListener('click', onBackdropClick);
				document.removeEventListener('keydown', onEscape);
			}

			function onCancel() {
				cleanup();
				closeModal(modal);
				resolve(false);
			}

			function onConfirm() {
				cleanup();
				closeModal(modal);
				resolve(true);
			}

			function onBackdropClick(event) {
				if (event.target === modal) {
					onCancel();
				}
			}

			function onEscape(event) {
				if (event.key === 'Escape') {
					onCancel();
				}
			}

			cancelBtn.addEventListener('click', onCancel);
			okBtn.addEventListener('click', onConfirm);
			modal.addEventListener('click', onBackdropClick);
			document.addEventListener('keydown', onEscape);
			openModal(modal);
			okBtn.focus();
		});
	}

	function initAdminSidebar() {
		var sidebar = document.getElementById('adminSidebar');
		var overlay = document.getElementById('mobileSidebarOverlay');
		var openBtn = document.getElementById('mobileSidebarToggle');
		var closeBtn = document.getElementById('mobileSidebarClose');

		if (!sidebar || !overlay || !openBtn) {
			return;
		}

		function openSidebar() {
			sidebar.classList.remove('-translate-x-full');
			overlay.classList.remove('hidden');
			document.body.classList.add('overflow-hidden');
			openBtn.setAttribute('aria-expanded', 'true');
		}

		function closeSidebar() {
			sidebar.classList.add('-translate-x-full');
			overlay.classList.add('hidden');
			document.body.classList.remove('overflow-hidden');
			openBtn.setAttribute('aria-expanded', 'false');
		}

		openBtn.addEventListener('click', openSidebar);
		overlay.addEventListener('click', closeSidebar);
		if (closeBtn) {
			closeBtn.addEventListener('click', closeSidebar);
		}

		sidebar.addEventListener('click', function (event) {
			var target = event.target;
			if (!(target instanceof Element)) {
				return;
			}

			var navLink = target.closest('.admin-nav-link');
			if (navLink && window.innerWidth < 768) {
				closeSidebar();
			}
		});

		document.addEventListener('keydown', function (event) {
			if (event.key === 'Escape' && !sidebar.classList.contains('-translate-x-full')) {
				closeSidebar();
			}
		});

		window.addEventListener('resize', function () {
			if (window.innerWidth >= 768) {
				overlay.classList.add('hidden');
				document.body.classList.remove('overflow-hidden');
				sidebar.classList.remove('-translate-x-full');
				openBtn.setAttribute('aria-expanded', 'false');
			} else {
				sidebar.classList.add('-translate-x-full');
			}
		});
	}

	window.samvadUi = window.samvadUi || {};
	window.samvadUi.confirm = confirm;

	document.addEventListener('DOMContentLoaded', function () {
		initAdminSidebar();
	});
})();
